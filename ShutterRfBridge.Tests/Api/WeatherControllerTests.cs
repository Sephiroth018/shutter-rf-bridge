using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Api;

public sealed class WeatherControllerTests : IAsyncDisposable
{
    private static readonly DateTimeOffset StepTime = NowcastResponseBuilder.CurrentStepTime;
    private static readonly DateTimeOffset Now = StepTime.AddMinutes(14);

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
        _application.NowcastHandler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime, 0.3).Build());

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("0.3", await ReadBody(response));
    }

    [Fact]
    public async Task Rain_UnsuccessfulStatus_ReturnsUnknown()
    {
        _application.NowcastHandler.Respond(HttpStatusCode.TooManyRequests, string.Empty);

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
        _application.NowcastHandler.Respond(HttpStatusCode.OK, "{");

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal("unknown", await ReadBody(response));
    }

    [Fact]
    public async Task Rain_UnusableLocation_ReturnsAnErrorWithTheProblemAsPlainText()
    {
        _application.SetSetting("Weather:Location:Latitude", "east");

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.StartsWith("text/plain", response.ContentType, StringComparison.Ordinal);
        Assert.Equal("Weather:Location:Latitude \"east\" is not a number.", await ReadBody(response));
    }

    private static async Task<string> ReadBody(HttpResponse response)
    {
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
