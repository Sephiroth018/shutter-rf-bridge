using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Transmission;

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

    [Fact]
    public void Main_Stopping_WaitsLongerThanTheLongestRunTime()
    {
        var hostOptions = _application.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.True(hostOptions.ShutdownTimeout > ShutterMove.MaxRunTime);
    }

    [Fact]
    public void Main_AnyWorkingDirectory_ReadsConfigurationFromTheProgramsFolder()
    {
        var environment = _application.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
    }
}
