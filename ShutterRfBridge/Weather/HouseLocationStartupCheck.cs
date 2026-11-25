namespace ShutterRfBridge.Weather;

/// <summary>Logs an error at startup when the <see cref="HouseLocation" /> is unusable; the shutters work anyway.</summary>
internal sealed class HouseLocationStartupCheck : IHostedService
{
    private readonly HouseLocation _location;
    private readonly RainReportLog _log;

    public HouseLocationStartupCheck(HouseLocation location, RainReportLog log)
    {
        _location = location;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_location is UnusableHouseLocation unusable)
        {
            _log.RecordUnusableLocationAtStartup(unusable.Problem);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
