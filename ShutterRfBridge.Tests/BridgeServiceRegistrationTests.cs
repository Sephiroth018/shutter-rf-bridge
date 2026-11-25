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
    [InlineData("47.5", "")]
    [InlineData("47.5", "east")]
    [InlineData("47.5", null)]
    [InlineData("91", "13.25")]
    public void AddBridgeServices_UnusableLocation_PassesTheStartupValidation(string latitude, string? longitude)
    {
        _settings["Weather:Location:Latitude"] = latitude;
        _settings["Weather:Location:Longitude"] = longitude;

        var exception = Record.Exception(ValidateStartup);

        Assert.Null(exception);
    }

    [Fact]
    public void AddBridgeServices_UnusableLocation_ProvidesItsProblem()
    {
        _settings["Weather:Location:Latitude"] = "47.5";

        var location = ResolveHouseLocation();

        Assert.Equal(new UnusableHouseLocation("Weather:Location:Longitude is missing or empty."), location);
    }

    private void ValidateStartup()
    {
        using var provider = BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    private HouseLocation ResolveHouseLocation()
    {
        using var provider = BuildServiceProvider();

        return provider.GetRequiredService<HouseLocation>();
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
