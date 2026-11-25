namespace ShutterRfBridge.Weather;

/// <summary>Logs an error at startup when the <see cref="HouseLocation" /> is unusable; the shutters work anyway.</summary>
internal sealed partial class HouseLocationStartupCheck : IHostedService
{
    private readonly HouseLocation _location;
    private readonly ILogger<HouseLocationStartupCheck> _logger;

    public HouseLocationStartupCheck(HouseLocation location, ILogger<HouseLocationStartupCheck> logger)
    {
        _location = location;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_location is UnusableHouseLocation unusable)
        {
            LogUnusable(unusable.Problem);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Error, Message = "The rain is unavailable until the location is fixed: {Problem}")]
    private partial void LogUnusable(string problem);
}
