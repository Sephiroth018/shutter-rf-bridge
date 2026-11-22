using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ShutterRfBridge.Configuration;

namespace ShutterRfBridge.Tests.Configuration;

public sealed class SettingsFileConfigurationTests : IDisposable
{
    private readonly DirectoryInfo _builtInDirectory = Directory.CreateTempSubdirectory();

    // Unique per test, so no other test sees or sets the environment variable of that name.
    private readonly string _key = $"SettingsFileTest{Guid.NewGuid():N}";
    private readonly DirectoryInfo _settingsDirectory = Directory.CreateTempSubdirectory();

    public SettingsFileConfigurationTests()
    {
        WriteValue(_builtInDirectory, "built-in");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_key, null);
        _builtInDirectory.Delete(true);
        _settingsDirectory.Delete(true);
    }

    [Fact]
    public void AddSettingsFile_ValueInBuiltInAndSettingsFile_TakesTheSettingsFilesValue()
    {
        WriteValue(_settingsDirectory, "settings file");
        using var configuration = CreateServiceConfiguration();

        configuration.AddSettingsFile(_settingsDirectory.FullName);

        Assert.Equal("settings file", configuration[_key]);
    }

    [Fact]
    public void AddSettingsFile_ValueInSettingsFileAndEnvironment_TakesTheEnvironmentVariable()
    {
        WriteValue(_settingsDirectory, "settings file");
        Environment.SetEnvironmentVariable(_key, "environment");
        using var configuration = CreateServiceConfiguration();

        configuration.AddSettingsFile(_settingsDirectory.FullName);

        Assert.Equal("environment", configuration[_key]);
    }

    [Fact]
    public void AddSettingsFile_NoDirectory_KeepsTheBuiltInValue()
    {
        using var configuration = CreateServiceConfiguration();

        configuration.AddSettingsFile(null);

        Assert.Equal("built-in", configuration[_key]);
    }

    [Fact]
    public void AddSettingsFile_DirectoryWithoutFile_ThrowsFileNotFound()
    {
        using var configuration = CreateServiceConfiguration();

        Assert.Throws<FileNotFoundException>(() => configuration.AddSettingsFile(_settingsDirectory.FullName));
    }

    [Fact]
    public void AddSettingsFile_DeployTemplate_ReadsItsCommentedJson()
    {
        var deployDirectory = Path.Combine(FindRepository().FullName, "deploy");
        using var configuration = CreateServiceConfiguration();

        configuration.AddSettingsFile(deployDirectory);

        Assert.Equal("http://0.0.0.0:5080", configuration["Urls"]);
    }

    private static DirectoryInfo FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "ShutterRfBridge.slnx")))
        {
            directory = directory.Parent ??
                        throw new DirectoryNotFoundException("The tests run outside the repository.");
        }

        return directory;
    }

    /// <summary>The configuration sources the service's own builder has, with the built-in files from the test's folder.</summary>
    private ConfigurationManager CreateServiceConfiguration() =>
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = _builtInDirectory.FullName, EnvironmentName = Environments.Production
        }).Configuration;

    private void WriteValue(DirectoryInfo directory, string value)
    {
        File.WriteAllText(Path.Combine(directory.FullName, SettingsFileConfiguration.FileName),
            $$"""{ "{{_key}}": "{{value}}" }""");
    }
}
