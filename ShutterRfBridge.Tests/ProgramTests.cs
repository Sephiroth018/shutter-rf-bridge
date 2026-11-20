using Microsoft.Extensions.Options;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests;

public sealed class ProgramTests : IAsyncDisposable
{
    private readonly BridgeApplication _application = new();

    public async ValueTask DisposeAsync()
    {
        await _application.DisposeAsync();
    }

    [Fact]
    public async Task Main_MissingRemoteId_FailsTheStartup()
    {
        _application.SetSetting("Bridge:RemoteId", string.Empty);

        await Assert.ThrowsAsync<OptionsValidationException>(() => _application.PostAsync("/shutters/3/stop"));
    }
}
