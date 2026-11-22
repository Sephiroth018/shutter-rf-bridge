using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;

namespace ShutterRfBridge.Configuration;

/// <summary>The house's settings file on the Pi, in the folder that systemd's <c>ConfigurationDirectory=</c> creates.</summary>
internal static class SettingsFileConfiguration
{
    /// <summary>The environment variable in which systemd passes the folder.</summary>
    public const string DirectoryVariable = "CONFIGURATION_DIRECTORY";

    public const string FileName = "appsettings.json";

    /// <summary>
    ///     Adds <see cref="FileName" /> in <paramref name="directory" /> above the built-in <c>appsettings*.json</c> and
    ///     below the environment variables, so those can still override it. Without a folder, adds nothing.
    /// </summary>
    /// <exception cref="FileNotFoundException">The folder has no settings file.</exception>
    public static void AddSettingsFile(this IConfigurationBuilder configuration, string? directory)
    {
        if (directory is null)
        {
            return;
        }

        // The prefixed DOTNET_ and ASPNETCORE_ variables come before the built-in files and rank below them.
        var environmentVariables = configuration.Sources.OfType<EnvironmentVariablesConfigurationSource>()
            .Single(source => string.IsNullOrEmpty(source.Prefix));
        var settingsFile = new JsonConfigurationSource { Path = Path.Combine(directory, FileName) };
        settingsFile.ResolveFileProvider();
        configuration.Sources.Insert(configuration.Sources.IndexOf(environmentVariables), settingsFile);
    }
}
