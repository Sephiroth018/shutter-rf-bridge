using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class HouseLocationStartupCheckTests
{
    private readonly FakeLogger<HouseLocationStartupCheck> _logger = new();

    [Fact]
    public async Task StartAsync_UnusableLocation_LogsAnErrorWithTheProblem()
    {
        var check = new HouseLocationStartupCheck(new UnusableHouseLocation("Weather:Location is missing."), _logger);

        await check.StartAsync(TestContext.Current.CancellationToken);

        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(
            (LogLevel.Error, "The rain is unavailable until the location is fixed: Weather:Location is missing."),
            (record.Level, record.Message));
    }

    [Fact]
    public async Task StartAsync_UsableLocation_LogsNothing()
    {
        var check = new HouseLocationStartupCheck(new UsableHouseLocation(new Coordinates(47.5, 13.25)), _logger);

        await check.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, _logger.Collector.Count);
    }
}
