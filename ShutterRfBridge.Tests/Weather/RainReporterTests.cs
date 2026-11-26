using System.Net;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
    public async Task ReportAsync_LaterStepWithoutValue_ReturnsTheHeaviestOfTheOthers()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, 0.1)
            .WithStep(StepTime.AddMinutes(15), null)
            .Build());

        var millimetres = await ReportAsync();

        Assert.Equal(0.1, millimetres);
    }

    [Fact]
    public async Task ReportAsync_CurrentStepWithoutValue_ReturnsTheLaterRain()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, null)
            .WithStep(StepTime.AddMinutes(15), 0.3)
            .Build());

        var millimetres = await ReportAsync();

        Assert.Equal(0.3, millimetres);
    }

    [Fact]
    public async Task ReportAsync_RunNotCoveringTheCurrentStep_ReturnsUnknownAndLogsTheStep()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build());

        var millimetres = await ReportAsync();

        Assert.Null(millimetres);
        Assert.Equal("No rain data: no values from 2026-11-23 18:30:00Z",
            _logger.LatestRecord.Message);
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
            "No rain data: stale run from 2026-11-23 18:15:00Z",
            _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task ReportAsync_NoStepWithValue_ReturnsUnknownAndLogsTheFirstStep()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, null)
            .WithStep(StepTime.AddMinutes(15), null)
            .WithStep(StepTime.AddMinutes(30), null)
            .Build());

        var millimetres = await ReportAsync();

        Assert.Null(millimetres);
        Assert.Equal("No rain data: no values from 2026-11-23 18:30:00Z",
            _logger.LatestRecord.Message);
    }

    [Fact]
    public async Task ReportAsync_ResponseBrokeOff_ReturnsUnknownAndLogsTheFailure()
    {
        _handler.SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpIOException(HttpRequestError.ResponseEnded));

        var millimetres = await ReportAsync();

        Assert.Null(millimetres);
        Assert.StartsWith("No rain data: request failed: ", _logger.LatestRecord.Message, StringComparison.Ordinal);
    }

    private Task<double?> ReportAsync() =>
        new RainReporter(new NowcastClient(_httpClient), _timeProvider, new RainReportLog(_logger))
            .ReportAsync(new Coordinates(47.5, 13.25), TestContext.Current.CancellationToken);
}
