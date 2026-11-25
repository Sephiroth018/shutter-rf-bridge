namespace ShutterRfBridge.Weather;

/// <summary>
///     The rain's log entries: each amount at debug level, since the CCU3 asks every 15 minutes, and missing rain data
///     as a warning every time; an invalid location at the level its caller gives.
/// </summary>
public partial class RainReportLog
{
    private readonly ILogger<RainReportLog> _logger;

    public RainReportLog(ILogger<RainReportLog> logger)
    {
        _logger = logger;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rain: {Millimetres} mm")]
    public partial void RecordAmount(double millimetres);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No rain data: {Reason}")]
    public partial void RecordUnknown(string reason);

    [LoggerMessage(Message = "No rain data, invalid location: {Problem}")]
    public partial void RecordInvalidLocation(LogLevel level, string problem);
}
