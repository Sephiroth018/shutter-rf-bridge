using Microsoft.AspNetCore.Http;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Tests.Api;

public sealed class ShuttersControllerTests : IAsyncDisposable
{
    private readonly BridgeApplication _application = new();

    public async ValueTask DisposeAsync()
    {
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task Up_ValidRoute_Returns202AndQueuesTheMove()
    {
        var status = await _application.PostAsync("/shutters/3/up/500/10000");

        Assert.Equal(StatusCodes.Status202Accepted, status);
        Assert.Equal(new ShutterMove(3, RemoteCommand.Up, 500, TimeSpan.FromSeconds(10)), await Dequeue());
    }

    [Theory]
    [InlineData(-30536, 35000)]
    [InlineData(-32768, 32768)]
    [InlineData(-1, 65535)]
    [InlineData(32767, 32767)]
    public async Task Up_SignedRunTime_QueuesItReadAsUnsigned(int runtimeMs, int expectedMs)
    {
        await _application.PostAsync($"/shutters/3/up/500/{runtimeMs}");

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), Assert.IsType<ShutterMove>(await Dequeue()).RunTime);
    }

    [Fact]
    public async Task Down_ValidRoute_Returns202AndQueuesTheMove()
    {
        var status = await _application.PostAsync("/shutters/0/down/1000/20000");

        Assert.Equal(StatusCodes.Status202Accepted, status);
        Assert.Equal(new ShutterMove(0, RemoteCommand.Down, 1000, TimeSpan.FromSeconds(20)), await Dequeue());
    }

    [Fact]
    public async Task Stop_ValidRoute_Returns202AndQueuesAStop()
    {
        var status = await _application.PostAsync("/shutters/15/stop");

        Assert.Equal(StatusCodes.Status202Accepted, status);
        Assert.Equal(new ShutterStop(15), await Dequeue());
    }

    [Theory]
    [InlineData("/shutters/16/up/500/10000")]
    [InlineData("/shutters/3/up/1001/10000")]
    [InlineData("/shutters/3/up/-1/10000")]
    [InlineData("/shutters/3/up/x/10000")]
    [InlineData("/shutters/3/up/500/0")]
    [InlineData("/shutters/3/up/500/-32769")]
    [InlineData("/shutters/3/up/500/32768")]
    public async Task Up_InvalidRouteValue_Returns400(string path)
    {
        var status = await _application.PostAsync(path);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
    }

    [Theory]
    [InlineData("/shutters/-1/down/500/10000")]
    [InlineData("/shutters/3/down/1001/10000")]
    [InlineData("/shutters/3/down/500/0")]
    [InlineData("/shutters/3/down/500/32768")]
    [InlineData("/shutters/3/down/500/x")]
    public async Task Down_InvalidRouteValue_Returns400(string path)
    {
        var status = await _application.PostAsync(path);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
    }

    [Theory]
    [InlineData("/shutters/16/stop")]
    [InlineData("/shutters/-1/stop")]
    [InlineData("/shutters/x/stop")]
    public async Task Stop_InvalidChannel_Returns400(string path)
    {
        var status = await _application.PostAsync(path);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
    }

    private Task<ShutterCommand> Dequeue() => _application.Queue.DequeueAsync(TestContext.Current.CancellationToken);
}
