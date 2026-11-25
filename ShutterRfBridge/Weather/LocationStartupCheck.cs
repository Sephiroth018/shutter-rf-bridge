namespace ShutterRfBridge.Weather;

/// <summary>Logs an error at startup when the <see cref="Location" /> is unusable; the shutters work anyway.</summary>
internal sealed class LocationStartupCheck : IHostedService
{
    private readonly Location _location;
    private readonly RainReportLog _log;

    public LocationStartupCheck(Location location, RainReportLog log)
    {
        _location = location;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_location is InvalidLocation unusable)
        {
            _log.RecordUnusableLocationAtStartup(unusable.Problem);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
