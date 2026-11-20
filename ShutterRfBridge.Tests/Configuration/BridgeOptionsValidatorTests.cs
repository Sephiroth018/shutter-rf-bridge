using ShutterRfBridge.Configuration;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Configuration;

public sealed class BridgeOptionsValidatorTests
{
    private static readonly BridgeOptions ValidOptions =
        new() { RemoteId = MadeUpRemote.Id, AllowedClients = ["192.0.2.10", "::1"] };

    private readonly BridgeOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, ValidOptions);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MissingRemoteId_FailsNamingIt()
    {
        var result = _validator.Validate(null, new BridgeOptions { AllowedClients = ValidOptions.AllowedClients });

        Assert.Equal(["Bridge:RemoteId is missing."], result.Failures);
    }

    [Fact]
    public void Validate_RemoteIdAboveTwentyEightBits_FailsNamingIt()
    {
        var result = _validator.Validate(null, new BridgeOptions
        {
            RemoteId = 0x10000000, AllowedClients = ValidOptions.AllowedClients
        });

        Assert.Equal(["Bridge:RemoteId 0x10000000 is above the maximum 0xFFFFFFF."], result.Failures);
    }

    [Fact]
    public void Validate_NoAllowedClients_FailsNamingThem()
    {
        var result = _validator.Validate(null, new BridgeOptions { RemoteId = MadeUpRemote.Id });

        Assert.Equal(["Bridge:AllowedClients is empty."], result.Failures);
    }

    [Fact]
    public void Validate_AllowedClientNotAnIpAddress_FailsNamingTheEntry()
    {
        var result = _validator.Validate(null, new BridgeOptions
        {
            RemoteId = MadeUpRemote.Id, AllowedClients = ["192.0.2.10", "ccu3.local"]
        });

        Assert.Equal(["Bridge:AllowedClients entry 'ccu3.local' isn't an IP address."], result.Failures);
    }
}
