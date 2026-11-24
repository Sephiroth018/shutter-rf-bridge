using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class RainReportLogTests
{
    private readonly RainReportLog _log;
    private readonly FakeLogger<RainReportLog> _logger = new();

    public RainReportLogTests()
    {
        _log = new RainReportLog(_logger);
    }

    [Fact]
    public void RecordAmount_AnyAmount_LogsItAtDebug()
    {
        _log.RecordAmount(0.3);

        Assert.Equal([LogLevel.Debug], LoggedLevels());
    }

    [Fact]
    public void RecordUnknown_FirstUnknown_LogsAWarningWithTheReason()
    {
        _log.RecordUnknown("the nowcast request failed");

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal((LogLevel.Warning, "The rain is now unknown: the nowcast request failed"),
            (record.Level, record.Message));
    }

    [Fact]
    public void RecordUnknown_UnknownAgain_LogsItOnlyAtDebug()
    {
        _log.RecordUnknown("the nowcast request failed");

        _log.RecordUnknown("the nowcast response is unreadable");

        Assert.Equal([LogLevel.Warning, LogLevel.Debug], LoggedLevels());
    }

    [Fact]
    public void RecordUnknown_AfterAnAmount_LogsAWarningAgain()
    {
        _log.RecordUnknown("the nowcast request failed");
        _log.RecordAmount(0);

        _log.RecordUnknown("the nowcast request failed");

        Assert.Equal([LogLevel.Warning, LogLevel.Debug, LogLevel.Warning], LoggedLevels());
    }

    private List<LogLevel> LoggedLevels() => [.. _logger.Collector.GetSnapshot().Select(record => record.Level)];
}
