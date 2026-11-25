namespace ShutterRfBridge.Weather;

/// <summary>
///     Logs every rain report at debug level, since the CCU3 asks every 15 minutes, and a warning when the rain becomes
///     unknown; further unknown reports during an outage stay at debug. An unusable location, which needs fixing, gets an
///     error at startup and a warning on every request.
/// </summary>
public partial class RainReportLog
{
    private readonly ILogger<RainReportLog> _logger;
    private bool _isUnknown;

    public RainReportLog(ILogger<RainReportLog> logger)
    {
        _logger = logger;
    }

    public void RecordAmount(double millimetres)
    {
        LogAmount(millimetres);
        _isUnknown = false;
    }

    public void RecordUnknown(string reason)
    {
        if (_isUnknown)
        {
            LogStillUnknown(reason);
            return;
        }

        _isUnknown = true;
        LogUnknown(reason);
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "The rain is unavailable until the location is fixed: {Problem}")]
    public partial void RecordUnusableLocationAtStartup(string problem);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The rain is unavailable: {Problem}")]
    public partial void RecordUnusableLocation(string problem);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reporting {Millimetres} mm of rain")]
    private partial void LogAmount(double millimetres);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The rain is still unknown: {Reason}")]
    private partial void LogStillUnknown(string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The rain is now unknown: {Reason}")]
    private partial void LogUnknown(string reason);
}
