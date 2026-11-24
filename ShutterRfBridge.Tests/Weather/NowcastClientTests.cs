using System.Net;
using System.Text.Json;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class NowcastClientTests : IDisposable
{
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
    public async Task GetStepAsync_AnyStep_RequestsTheStepAtTheCoordinates()
    {
        Uri? requested = null;
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build(),
            request => requested = request.RequestUri);

        await GetStepAsync();

        Assert.Equal(
            "https://dataset.api.hub.geosphere.at/v1/timeseries/forecast/nowcast-v1-15min-1km?parameters=rr&lat_lon=47.5,13.25&start=2026-11-23T18:30:00Z&end=2026-11-23T18:30:00Z",
            requested?.AbsoluteUri);
    }

    [Fact]
    public async Task GetStepAsync_StepTimeWithOffset_RequestsItInUtc()
    {
        Uri? requested = null;
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build(),
            request => requested = request.RequestUri);

        await _client.GetStepAsync(Location, StepTime.ToOffset(TimeSpan.FromHours(1)),
            TestContext.Current.CancellationToken);

        Assert.EndsWith("&start=2026-11-23T18:30:00Z&end=2026-11-23T18:30:00Z",
            requested?.AbsoluteUri,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStepAsync_ResponseWithTheStep_ReturnsTheRunAndItsMillimetres()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var nowcast = await GetStepAsync();

        Assert.Equal(new Nowcast(NowcastResponseBuilder.DefaultReferenceTime, 0.3), nowcast);
    }

    [Fact]
    public async Task GetStepAsync_StepWithoutValue_ReturnsNoMillimetres()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, null).Build());

        var nowcast = await GetStepAsync();

        Assert.Null(nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepAsync_RunNotCoveringTheStep_ReturnsNoMillimetres()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().Build());

        var nowcast = await GetStepAsync();

        Assert.Null(nowcast.Millimetres);
    }

    [Fact]
    public async Task GetStepAsync_StepOfAnotherTime_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime.AddMinutes(15), 0.3).Build());

        await Assert.ThrowsAsync<JsonException>(GetStepAsync);
    }

    [Fact]
    public async Task GetStepAsync_TwoSteps_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime, 0.1).WithStep(StepTime.AddMinutes(15), 0.3).Build());

        await Assert.ThrowsAsync<JsonException>(GetStepAsync);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetStepAsync_UnsuccessfulStatus_ThrowsHttpRequestException(HttpStatusCode status)
    {
        _handler.Respond(status, """{"detail":"Requested point is outside of dataset bounds!"}""");

        await Assert.ThrowsAsync<HttpRequestException>(GetStepAsync);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("""{"timestamps":[],"features":[]}""")]
    public async Task GetStepAsync_NotANowcast_ThrowsJsonException(string json)
    {
        _handler.Respond(HttpStatusCode.OK, json);

        await Assert.ThrowsAnyAsync<JsonException>(GetStepAsync);
    }

    [Fact]
    public async Task GetStepAsync_TwoPoints_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithFeatureCount(2).Build());

        await Assert.ThrowsAsync<JsonException>(GetStepAsync);
    }

    [Fact]
    public async Task GetStepAsync_MoreValuesThanTimestamps_ThrowsJsonException()
    {
        _handler.Respond(HttpStatusCode.OK, """
                                            {"reference_time":"2026-11-23T18:15+00:00","timestamps":[],
                                             "features":[{"properties":{"parameters":{"rr":{"data":[0.0]}}}}]}
                                            """);

        await Assert.ThrowsAsync<JsonException>(GetStepAsync);
    }

    private Task<Nowcast> GetStepAsync() =>
        _client.GetStepAsync(Location, StepTime, TestContext.Current.CancellationToken);
}
