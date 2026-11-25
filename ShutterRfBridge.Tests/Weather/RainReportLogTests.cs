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

        Assert.Equal([(LogLevel.Debug, "Rain: 0.3 mm")], LoggedEntries());
    }

    [Fact]
    public void RecordUnknown_UnknownAgain_LogsAWarningEachTime()
    {
        _log.RecordUnknown("request failed");

        _log.RecordUnknown("unreadable response");

        Assert.Equal(
            [
                (LogLevel.Warning, "No rain data: request failed"),
                (LogLevel.Warning, "No rain data: unreadable response")
            ],
            LoggedEntries());
    }

    [Fact]
    public void RecordInvalidLocation_AnyProblem_LogsAWarningWithIt()
    {
        _log.RecordInvalidLocation("Weather:Location missing");

        Assert.Equal([(LogLevel.Warning, "No rain data, invalid location: Weather:Location missing")],
            LoggedEntries());
    }

    private List<(LogLevel, string)> LoggedEntries() =>
        [.. _logger.Collector.GetSnapshot().Select(record => (record.Level, record.Message))];
}
