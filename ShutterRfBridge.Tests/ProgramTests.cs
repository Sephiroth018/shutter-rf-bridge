using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [Theory]
    [InlineData("")]
    [InlineData("east")]
    public async Task Main_UnreadableCoordinate_FailsTheStartup(string longitude)
    {
        _application.SetSetting("Weather:Location:Longitude", longitude);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _application.PostAsync("/shutters/3/stop"));
    }

    [Fact]
    public async Task Main_CoordinateOutOfRange_FailsTheStartup()
    {
        _application.SetSetting("Weather:Location:Latitude", "91");

        await Assert.ThrowsAsync<OptionsValidationException>(() => _application.PostAsync("/shutters/3/stop"));
    }

    [Fact]
    public void Main_AnyWorkingDirectory_ReadsConfigurationFromTheProgramsFolder()
    {
        var environment = _application.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
    }
}
