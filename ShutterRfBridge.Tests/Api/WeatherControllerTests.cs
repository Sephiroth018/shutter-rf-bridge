using System.Net;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class RainEndpointTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 18, 44, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StepTime = new(2026, 11, 23, 18, 30, 0, TimeSpan.Zero);

    private readonly BridgeApplication _application = new();

    public RainEndpointTests()
    {
        var timeProvider = new TimeProviderSubstitute().TimeProvider;
        timeProvider.GetUtcNow().Returns(Now);
        _application.ReplaceService(timeProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task Get_CurrentStep_ReturnsItsAmountAsPlainText()
    {
        Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("0.3", await ReadBody(response));
    }

    [Fact]
    public async Task Get_UnsuccessfulStatus_ReturnsUnknown()
    {
        Respond(HttpStatusCode.TooManyRequests, string.Empty);

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Get_RequestTimedOut_ReturnsUnknown()
    {
        _application.NowcastHandler.SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Get_UnreadableResponse_ReturnsUnknown()
    {
        Respond(HttpStatusCode.OK, "{");

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Get_StaleRun_ReturnsUnknown()
    {
        Respond(HttpStatusCode.OK, new NowcastResponseBuilder()
            .WithReferenceTime(Now.AddHours(-2))
            .WithStep(StepTime, 0.3)
            .Build());

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Get_NoLocation_ReturnsUnknownWithoutARequest()
    {
        _application.SetSetting("Weather:Location:Latitude", string.Empty);
        _application.SetSetting("Weather:Location:Longitude", string.Empty);

        var response = await _application.GetAsync(RainEndpoint.Path);

        Assert.Equal("unknown", await ReadBody(response));
        await _application.NowcastHandler.DidNotReceive()
            .SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    private void Respond(HttpStatusCode status, string json)
    {
        _application.NowcastHandler.SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => new HttpResponseMessage(status) { Content = new StringContent(json) });
    }

    private static async Task<string> ReadBody(HttpResponse response)
    {
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
