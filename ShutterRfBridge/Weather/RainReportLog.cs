namespace ShutterRfBridge.Weather;

/// <summary>
///     Logs every report at debug level and only changes of its <see cref="RainCondition" /> above it, since the CCU3
///     asks every 15 minutes. A change to unknown is a warning; further unknown reports during an outage aren't.
/// </summary>
public partial class RainReportLog
{
    private readonly Lock _lock = new();
    private readonly ILogger<RainReportLog> _logger;
    private RainCondition? _condition;

    public RainReportLog(ILogger<RainReportLog> logger)
    {
        _logger = logger;
    }

    public void Record(RainReport report)
    {
        LogReport(report);
        RainCondition? previous;
        lock (_lock)
        {
            previous = _condition;
            _condition = report.Condition;
        }

        if (previous == report.Condition)
        {
            return;
        }

        if (report is RainUnknown unknown)
        {
            LogUnknown(unknown.Reason);
        }
        else
        {
            LogChanged(report.Condition, report);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reporting {Report}")]
    private partial void LogReport(RainReport report);

    [LoggerMessage(Level = LogLevel.Information, Message = "The rain is now {Condition}: {Report}")]
    private partial void LogChanged(RainCondition condition, RainReport report);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The rain is now unknown: {Reason}")]
    private partial void LogUnknown(string reason);
}
