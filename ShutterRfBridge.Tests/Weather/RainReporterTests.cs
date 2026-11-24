using System.Net;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class RainReporterTests : IDisposable
{
    private static readonly DateTimeOffset StepTime = NowcastResponseBuilder.CurrentStepTime;

    private readonly SubstitutableHttpMessageHandler _handler = Substitute.For<SubstitutableHttpMessageHandler>();
    private readonly HttpClient _httpClient;
    private readonly FakeLogger<RainReportLog> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(StepTime);

    public RainReporterTests()
    {
        _httpClient = new HttpClient(_handler);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(14.999)]
    public async Task ReportAsync_TimeWithinTheStep_ReturnsThatStepsMillimetres(double minutesIntoStep)
    {
        _timeProvider.SetUtcNow(StepTime.AddMinutes(minutesIntoStep));
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var millimetres = await ReportAsync();

        Assert.Equal(0.3, millimetres);
    }

    [Fact]
    public async Task ReportAsync_HeavierRainInTheNextHalfHour_ReturnsIt()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, 0)
            .WithStep(StepTime.AddMinutes(15), 0.2)
            .WithStep(StepTime.AddMinutes(30), 0.4)
            .Build());

        var millimetres = await ReportAsync();

        Assert.Equal(0.4, millimetres);
    }

    [Fact]
    public async Task ReportAsync_RunExactlyAnHourOld_ReturnsTheMillimetres()
    {
        _timeProvider.SetUtcNow(NowcastResponseBuilder.DefaultReferenceTime.AddHours(1));
        _handler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime.AddMinutes(45), 0.3).Build());

        var millimetres = await ReportAsync();

        Assert.Equal(0.3, millimetres);
    }

    [Fact]
    public async Task ReportAsync_RunOlderThanAnHour_ReturnsUnknownAndLogsTheRun()
    {
        _timeProvider.SetUtcNow(NowcastResponseBuilder.DefaultReferenceTime.AddMinutes(61));
        _handler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime.AddMinutes(45), 0.3).Build());

        var millimetres = await ReportAsync();

        Assert.Null(millimetres);
        Assert.Equal(
            "The rain is now unknown: the latest nowcast run, from 2026-11-23 18:15:00Z, is older than 01:00:00",
            _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task ReportAsync_NoValueForTheStep_ReturnsUnknownAndLogsTheStep()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, null).Build());

        var millimetres = await ReportAsync();

        Assert.Null(millimetres);
        Assert.Equal("The rain is now unknown: the nowcast has no value for 2026-11-23 18:30:00Z",
            _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task ReportAsync_NoLocation_ReturnsUnknownWithoutARequest()
    {
        var millimetres = await CreateReporter(null).ReportAsync(TestContext.Current.CancellationToken);

        Assert.Null(millimetres);
        await _handler.DidNotReceive().SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    private Task<double?> ReportAsync() =>
        CreateReporter(new Coordinates(47.5, 13.25)).ReportAsync(TestContext.Current.CancellationToken);

    private RainReporter CreateReporter(Coordinates? location) =>
        new(new NowcastClient(_httpClient),
            Options.Create(new WeatherOptions { Location = location }),
            _timeProvider, new RainReportLog(_logger));
}
