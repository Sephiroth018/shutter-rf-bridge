using Microsoft.Extensions.Options;
using ShutterRfBridge.Configuration;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Transmission;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge;

/// <summary>The service's own registrations, apart from the host's logging and systemd integration.</summary>
internal static class BridgeServiceRegistration
{
    /// <summary>
    ///     Registers the shutters' and the rain's services; in the Development environment, the radio is a dry run.
    /// </summary>
    public static IServiceCollection AddBridgeServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddControllers();
        services.AddOptions<BridgeOptions>().BindConfiguration(BridgeOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<BridgeOptions>, BridgeOptionsValidator>();
        // Not validated on start: the shutters work without the rain, so an invalid location is only logged.
        services.AddSingleton(provider => Location.Read(provider.GetRequiredService<IConfiguration>()));
        services.AddHostedService<LocationStartupCheck>();
        services.AddHttpClient<NowcastClient>(client => client.Timeout = NowcastClient.RequestTimeout)
            // Its default logging writes every request at info level, which would reach the SD card every 15 minutes.
            .RemoveAllLoggers();
        services.AddTransient<RainReporter>();
        services.AddSingleton<RainReportLog>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TransmitQueue>();
        services.AddHostedService<TransmitWorker>();
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IRemoteTransmitter, DryRunTransmitter>();
        }
        else
        {
            services.AddSingleton<ISpiBus, SpiDeviceBus>();
            services.AddSingleton<IRemoteTransmitter, Cc1101Transmitter>();
        }

        return services;
    }
}
