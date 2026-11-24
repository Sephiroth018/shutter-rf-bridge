using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Api;

public sealed class WeatherControllerTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 18, 44, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StepTime = new(2026, 11, 23, 18, 30, 0, TimeSpan.Zero);

    private readonly BridgeApplication _application = new();

    public WeatherControllerTests()
    {
        _application.ReplaceService<TimeProvider>(new FakeTimeProvider(Now));
    }

    public async ValueTask DisposeAsync()
    {
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task Rain_CurrentStep_ReturnsItsAmountAsPlainText()
    {
        Respond(HttpStatusCode.OK, new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("0.3", await ReadBody(response));
    }

    [Fact]
    public async Task Rain_UnsuccessfulStatus_ReturnsUnknown()
    {
        Respond(HttpStatusCode.TooManyRequests, string.Empty);

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Rain_RequestTimedOut_ReturnsUnknown()
    {
        _application.NowcastHandler.SendRequestAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Rain_UnreadableResponse_ReturnsUnknown()
    {
        Respond(HttpStatusCode.OK, "{");

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal("unknown", await ReadBody(response));
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
