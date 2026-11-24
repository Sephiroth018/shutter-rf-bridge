using System.Net;
using System.Text.Json;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class NowcastClientTests : IDisposable
{
    private const int StepCount = 3;
    private static readonly DateTimeOffset StepTime = NowcastResponseBuilder.CurrentStepTime;
    private static readonly Coordinates Location = new(47.5, 13.25);

    private readonly NowcastClient _client;
    private readonly SubstitutableHttpMessageHandler _handler = Substitute.For<SubstitutableHttpMessageHandler>();
    private readonly HttpClient _httpClient;

    public NowcastClientTests()
    {
        _httpClient = new HttpClient(_handler);
        _client = new NowcastClient(_httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetStepsAsync_AnySteps_RequestsThemAtTheCoordinates()
    {
        Uri? requested = null;
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build(),
            request => requested = request.RequestUri);

        await GetStepsAsync();

        Assert.Equal(
            "https://dataset.api.hub.geosphere.at/v1/timeseries/forecast/nowcast-v1-15min-1km?parameters=rr&lat_lon=47.5,13.25&start=2026-11-23T18:30:00.0000000Z&end=2026-11-23T19:00:00.0000000Z",
            requested?.AbsoluteUri);
    }

    [Fact]
    public async Task GetStepsAsync_StepTimeWithOffset_RequestsItInUtc()
    {
        Uri? requested = null;
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build(),
            request => requested = request.RequestUri);

        await _client.GetStepsAsync(Location, StepTime.ToOffset(TimeSpan.FromHours(1)), StepCount,
            TestContext.Current.CancellationToken);

        Assert.EndsWith("&start=2026-11-23T18:30:00.0000000Z&end=2026-11-23T19:00:00.0000000Z",
            requested?.AbsoluteUri,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStepsAsync_AllSteps_ReturnsTheRunAndTheHeaviestRain()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, 0.1)
            .WithStep(StepTime.AddMinutes(15), 0.5)
            .WithStep(StepTime.AddMinutes(30), 0.3)
            .Build());

        var nowcast = await GetStepsAsync();

        Assert.Equal(new Nowcast(NowcastResponseBuilder.DefaultReferenceTime, 0.5), nowcast);
    }

    [Fact]
    public async Task GetStepsAsync_RunEndingAfterTheFirstStep_ReturnsItsRain()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var nowcast = await GetStepsAsync();

        Assert.Equal(0.3, nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepsAsync_LaterStepWithoutValue_ReturnsTheHeaviestOfTheOthers()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, 0.1)
            .WithStep(StepTime.AddMinutes(15), null)
            .Build());

        var nowcast = await GetStepsAsync();

        Assert.Equal(0.1, nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepsAsync_FirstStepWithoutValue_ReturnsNoMillimetres()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, null)
            .WithStep(StepTime.AddMinutes(15), 0.3)
            .Build());

        var nowcast = await GetStepsAsync();

        Assert.Null(nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepsAsync_RunNotCoveringTheSteps_ReturnsNoMillimetres()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build());

        var nowcast = await GetStepsAsync();

        Assert.Null(nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepsAsync_StepsOfAnotherTime_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime.AddMinutes(15), 0.3).Build());

        await Assert.ThrowsAsync<JsonException>(GetStepsAsync);
    }

    [Fact]
    public async Task GetStepsAsync_MoreStepsThanAskedFor_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithStep(StepTime, 0.1)
            .WithStep(StepTime.AddMinutes(15), 0.1)
            .WithStep(StepTime.AddMinutes(30), 0.1)
            .WithStep(StepTime.AddMinutes(45), 0.1)
            .Build());

        await Assert.ThrowsAsync<JsonException>(GetStepsAsync);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetStepsAsync_UnsuccessfulStatus_ThrowsHttpRequestException(HttpStatusCode status)
    {
        _handler.Respond(status, """{"detail":"Requested point is outside of dataset bounds!"}""");

        await Assert.ThrowsAsync<HttpRequestException>(GetStepsAsync);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("""{"timestamps":[],"features":[]}""")]
    public async Task GetStepsAsync_NotANowcast_ThrowsJsonException(string json)
    {
        _handler.Respond(HttpStatusCode.OK, json);

        await Assert.ThrowsAnyAsync<JsonException>(GetStepsAsync);
    }

    [Fact]
    public async Task GetStepsAsync_TwoPoints_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithFeatureCount(2).Build());

        await Assert.ThrowsAsync<JsonException>(GetStepsAsync);
    }

    [Fact]
    public async Task GetStepsAsync_MoreValuesThanTimestamps_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK, """
                                            {"reference_time":"2026-11-23T18:15+00:00","timestamps":[],
                                             "features":[{"properties":{"parameters":{"rr":{"data":[0.0]}}}}]}
                                            """);

        await Assert.ThrowsAsync<JsonException>(GetStepsAsync);
    }

    private Task<Nowcast> GetStepsAsync() =>
        _client.GetStepsAsync(Location, StepTime, StepCount, TestContext.Current.CancellationToken);
}
