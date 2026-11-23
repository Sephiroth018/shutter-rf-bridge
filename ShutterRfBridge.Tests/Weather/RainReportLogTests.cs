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
    public void Record_FirstAmount_LogsItAtDebugAndItsConditionAtInfo()
    {
        _log.Record(new RainAmount(0.3));

        Assert.Equal([LogLevel.Debug, LogLevel.Information], LoggedLevels());
    }

    [Fact]
    public void Record_SameConditionAgain_LogsItOnlyAtDebug()
    {
        _log.Record(new RainAmount(0.3));

        _log.Record(new RainAmount(0.5));

        Assert.Equal([LogLevel.Debug, LogLevel.Information, LogLevel.Debug], LoggedLevels());
    }

    [Fact]
    public void Record_DryToRaining_LogsTheChangeAtInfo()
    {
        _log.Record(new RainAmount(0));

        _log.Record(new RainAmount(0.3));

        Assert.Equal([LogLevel.Debug, LogLevel.Information, LogLevel.Debug, LogLevel.Information], LoggedLevels());
    }

    [Fact]
    public void Record_ChangeToUnknown_LogsAWarning()
    {
        _log.Record(new RainAmount(0));

        _log.Record(new RainUnknown("the nowcast request failed"));

        Assert.Equal([LogLevel.Debug, LogLevel.Information, LogLevel.Debug, LogLevel.Warning], LoggedLevels());
    }

    [Fact]
    public void Record_UnknownAgain_LogsNoSecondWarning()
    {
        _log.Record(new RainUnknown("the nowcast request failed"));

        _log.Record(new RainUnknown("the nowcast response is unreadable"));

        Assert.Equal([LogLevel.Debug, LogLevel.Warning, LogLevel.Debug], LoggedLevels());
    }

    private List<LogLevel> LoggedLevels() => [.. _logger.Collector.GetSnapshot().Select(record => record.Level)];
}
