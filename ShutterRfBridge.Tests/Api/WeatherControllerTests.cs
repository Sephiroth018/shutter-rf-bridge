using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

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

    [Theory]
    [InlineData(0, "0")]
    [InlineData(12.5, "12.5")]
    [InlineData(0.00001, "0")]
    [InlineData(1.23456, "1.235")]
    public async Task Rain_AnyAmount_ReturnsItWithoutExponent(double millimetres, string text)
    {
        _application.NowcastHandler.Respond(HttpStatusCode.OK,
            new NowcastResponseBuilder().WithStep(StepTime, millimetres).Build());

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal(text, await ReadBody(response));
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
        _application.NowcastHandler.Fail(new TaskCanceledException());

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
    public async Task Rain_InvalidLocation_ReturnsAnErrorWithoutTheProblem()
    {
        _application.SetSetting("Weather:Location:Latitude", "91");

        var response = await _application.GetAsync("/weather/rain");

        Assert.Equal((StatusCodes.Status500InternalServerError, string.Empty),
            (response.StatusCode, await ReadBody(response)));
    }

    [Fact]
    public async Task Rain_InvalidLocation_LogsTheProblemAtStartupAndWithTheRequest()
    {
        const string Message = "No rain data, invalid location: Weather:Location:Latitude 91 out of range";
        var logger = new FakeLogger<RainReportLog>();
        _application.ReplaceService(new RainReportLog(logger));
        _application.SetSetting("Weather:Location:Latitude", "91");

        await _application.GetAsync("/weather/rain");

        Assert.Equal([(LogLevel.Error, Message), (LogLevel.Warning, Message)],
            logger.Collector.GetSnapshot().Select(record => (record.Level, record.Message)));
    }

    private static async Task<string> ReadBody(HttpResponse response)
    {
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
