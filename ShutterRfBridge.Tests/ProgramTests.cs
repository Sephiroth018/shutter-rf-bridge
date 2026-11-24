using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    public void Main_AnyWorkingDirectory_ReadsConfigurationFromTheProgramsFolder()
    {
        var environment = _application.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal(AppContext.BaseDirectory, environment.ContentRootPath);
    }
}
