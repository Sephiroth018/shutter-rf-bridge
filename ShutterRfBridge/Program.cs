using Microsoft.Extensions.Options;
using NLog.Web;
using ShutterRfBridge;

var builder = WebApplication.CreateBuilder(args);

// NLog takes its targets and rules from the configuration's NLog section.
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { ShutdownOnDispose = true });

builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()));
builder.Services.AddOptions<BridgeOptions>().BindConfiguration(BridgeOptions.SectionName).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<BridgeOptions>, BridgeOptionsValidator>();
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
app.UseMiddleware<ClientAllowListMiddleware>();
app.MapControllers();

// A failed background service stops the host, which still ends with exit code 0; systemd restarts on a non-zero one.
// Resolved before running, since the host disposes its services when it stops.
var backgroundServices = app.Services.GetServices<IHostedService>().OfType<BackgroundService>().ToList();
await app.RunAsync();
return backgroundServices.Any(service => service.ExecuteTask is { IsFaulted: true }) ? 1 : 0;
