using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;

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
    [InlineData("")]
    [InlineData("east")]
    public void AddBridgeServices_UnreadableCoordinate_FailsTheStartupValidation(string longitude)
    {
        _settings["Weather:Location:Latitude"] = "47.5";
        _settings["Weather:Location:Longitude"] = longitude;

        Assert.Throws<InvalidOperationException>(ValidateStartup);
    }

    [Fact]
    public void AddBridgeServices_OnlyOneCoordinate_FailsTheStartupValidation()
    {
        _settings["Weather:Location:Latitude"] = "47.5";

        Assert.Throws<InvalidOperationException>(ValidateStartup);
    }

    [Fact]
    public void AddBridgeServices_CoordinateOutOfRange_FailsTheStartupValidation()
    {
        _settings["Weather:Location:Latitude"] = "91";
        _settings["Weather:Location:Longitude"] = "13.25";

        Assert.Throws<OptionsValidationException>(ValidateStartup);
    }

    private void ValidateStartup()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(_settings).Build())
            .AddBridgeServices(environment);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }
}
