using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests;

/// <summary>
///     Runs the startup validation without starting the service, since the test host can hide a failed startup's
///     exception behind an <see cref="ObjectDisposedException" />.
/// </summary>
public sealed class BridgeServiceRegistrationTests
{
    private readonly Dictionary<string, string?> _settings = new()
    {
        ["Bridge:RemoteId"] = MadeUpRemote.Id.ToString(CultureInfo.InvariantCulture),
        ["Bridge:AllowedClients:0"] = "127.0.0.1"
    };

    [Fact]
    public void AddBridgeServices_ValidSettings_PassesTheStartupValidation()
    {
        var exception = Record.Exception(ValidateStartup);

        Assert.Null(exception);
    }

    [Fact]
    public void AddBridgeServices_MissingRemoteId_FailsTheStartupValidation()
    {
        _settings.Remove("Bridge:RemoteId");

        Assert.Throws<OptionsValidationException>(ValidateStartup);
    }

    [Theory]
    [InlineData("Weather:Location:Longitude", "")]
    [InlineData("Weather:Location:Longitude", "east")]
    [InlineData("Weather:Location:Longitude", null)]
    [InlineData("Weather:Location:Latitude", "91")]
    public void AddBridgeServices_InvalidLocation_StartsWithTheLocationInvalid(string key, string? value)
    {
        _settings["Weather:Location:Latitude"] = "47.5";
        _settings[key] = value;

        var exception = Record.Exception(ValidateStartup);
        var location = ResolveLocation();

        Assert.Null(exception);
        Assert.IsType<InvalidLocation>(location);
    }

    [Fact]
    public void AddBridgeServices_LocationWrittenAsAValue_StartsWithTheLocationMissing()
    {
        _settings["Weather:Location"] = string.Empty;

        var exception = Record.Exception(ValidateStartup);
        var location = ResolveLocation();

        Assert.Null(exception);
        Assert.Equal(new InvalidLocation("Weather:Location missing"), location);
    }

    private void ValidateStartup()
    {
        using var provider = BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    private Location ResolveLocation()
    {
        using var provider = BuildServiceProvider();

        return provider.GetRequiredService<Location>();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        return new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build())
            .AddBridgeServices(environment)
            .BuildServiceProvider();
    }
}
