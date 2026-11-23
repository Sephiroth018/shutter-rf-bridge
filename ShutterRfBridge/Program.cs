using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using ShutterRfBridge.Api;
using ShutterRfBridge.Configuration;
using ShutterRfBridge.Logging;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Transmission;
using ShutterRfBridge.Weather;

// The config files sit next to the program, whatever folder it is started from.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args, ContentRootPath = AppContext.BaseDirectory
});
// Added before anything reads the configuration, NLog included.
builder.Configuration.AddSettingsFile(Environment.GetEnvironmentVariable(SettingsFileConfiguration.DirectoryVariable));

// Registered before NLog reads its section, whose journal target's layout uses the renderer.
LogManager.Setup().SetupExtensions(extensions => extensions.RegisterLayoutRenderer<JournalPriorityLayoutRenderer>());
// NLog takes its targets and rules from the configuration's NLog section.
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { ShutdownOnDispose = true });

// Tells systemd once the host has started and handles its stop signal; does nothing outside systemd.
builder.Services.AddSystemd();

builder.Services.AddControllers();
builder.Services.AddOptions<BridgeOptions>().BindConfiguration(BridgeOptions.SectionName).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<BridgeOptions>, BridgeOptionsValidator>();
builder.Services.AddOptions<WeatherOptions>().BindConfiguration(WeatherOptions.SectionName).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<WeatherOptions>, WeatherOptionsValidator>();
builder.Services.AddHttpClient<NowcastClient>((services, client) =>
    {
        client.BaseAddress = NowcastClient.BaseAddress;
        client.Timeout = services.GetRequiredService<IOptions<WeatherOptions>>().Value.RequestTimeout;
    })
    // Its default logging writes every request at info level, which would reach the SD card every 15 minutes.
    .RemoveAllLoggers();
builder.Services.AddTransient<RainReporter>();
builder.Services.AddSingleton<RainReportLog>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TransmitQueue>();
builder.Services.AddHostedService<TransmitWorker>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IRemoteTransmitter, DryRunTransmitter>();
}
else
{
    builder.Services.AddSingleton<ISpiBus, SpiDeviceBus>();
    builder.Services.AddSingleton<IRemoteTransmitter, Cc1101Transmitter>();
}

var app = builder.Build();
// After the allow-list, which logs its own refusals.
app.UseMiddleware<ClientAllowListMiddleware>();
app.UseMiddleware<RejectedRequestLoggingMiddleware>();
app.MapControllers();

// A failed background service stops the host, which still ends with exit code 0; systemd restarts on a non-zero one.
// Resolved before running, since the host disposes its services when it stops.
var backgroundServices = app.Services.GetServices<IHostedService>().OfType<BackgroundService>().ToList();
await app.RunAsync();
return backgroundServices.Any(service => service.ExecuteTask is { IsFaulted: true }) ? 1 : 0;
