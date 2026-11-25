using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class LocationStartupCheckTests
{
    private readonly FakeLogger<RainReportLog> _logger = new();

    [Fact]
    public async Task StartAsync_InvalidLocation_LogsAnErrorWithTheProblem()
    {
        var check = new LocationStartupCheck(new InvalidLocation("Weather:Location missing"),
            new RainReportLog(_logger));

        await check.StartAsync(TestContext.Current.CancellationToken);

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal((LogLevel.Error, "No rain data, invalid location: Weather:Location missing"),
            (record.Level, record.Message));
    }

    [Fact]
    public async Task StartAsync_ValidLocation_LogsNothing()
    {
        var check = new LocationStartupCheck(new ValidLocation(new Coordinates(47.5, 13.25)),
            new RainReportLog(_logger));

        await check.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, _logger.Collector.Count);
    }
}
