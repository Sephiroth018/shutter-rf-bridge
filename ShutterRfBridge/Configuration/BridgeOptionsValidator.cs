using System.Net;
using Microsoft.Extensions.Options;
using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Configuration;

/// <summary>Fails the startup when <see cref="BridgeOptions" /> are missing or invalid.</summary>
internal sealed class BridgeOptionsValidator : IValidateOptions<BridgeOptions>
{
    public ValidateOptionsResult Validate(string? name, BridgeOptions options)
    {
        List<string> failures = [];
        if (options.RemoteId is not { } remoteId)
        {
            failures.Add($"{BridgeOptions.SectionName}:{nameof(BridgeOptions.RemoteId)} is missing.");
        }
        else if (remoteId > RemoteFrame.MaxRemoteId)
        {
            failures.Add(
                $"{BridgeOptions.SectionName}:{nameof(BridgeOptions.RemoteId)} 0x{remoteId:X} is above the maximum 0x{RemoteFrame.MaxRemoteId:X}.");
        }

        if (options.AllowedClients.Count == 0)
        {
            failures.Add($"{BridgeOptions.SectionName}:{nameof(BridgeOptions.AllowedClients)} is empty.");
        }

        failures.AddRange(options.AllowedClients
            .Where(client => !IPAddress.TryParse(client, out _))
            .Select(client =>
                $"{BridgeOptions.SectionName}:{nameof(BridgeOptions.AllowedClients)} entry '{client}' isn't an IP address."));

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
