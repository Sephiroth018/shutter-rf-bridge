using System.Net;
using Microsoft.AspNetCore.Http;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests;

public sealed class ClientAllowListMiddlewareTests : IAsyncDisposable
{
    private readonly BridgeApplication _application = new();

    public async ValueTask DisposeAsync()
    {
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task InvokeAsync_ClientNotAllowed_Returns403()
    {
        var status = await _application.PostAsync("/shutters/3/stop", IPAddress.Parse("192.0.2.10"));

        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task InvokeAsync_NoClientAddress_Returns403()
    {
        var status = await _application.PostAsync("/shutters/3/stop", null);

        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task InvokeAsync_AllowedClientAsIpv4MappedIpv6Address_LetsItThrough()
    {
        var status = await _application.PostAsync("/shutters/3/stop", IPAddress.Loopback.MapToIPv6());

        Assert.Equal(StatusCodes.Status202Accepted, status);
    }

    [Fact]
    public async Task InvokeAsync_AllowedLinkLocalClientWithScopeId_LetsItThrough()
    {
        _application.SetSetting("Bridge:AllowedClients:0", "fe80::1");

        var status = await _application.PostAsync("/shutters/3/stop", IPAddress.Parse("fe80::1%2"));

        Assert.Equal(StatusCodes.Status202Accepted, status);
    }
}
