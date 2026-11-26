using System.Net;

namespace ShutterRfBridge.Configuration;

/// <summary>The local settings for the shutters, from the <see cref="SectionName" /> configuration section.</summary>
internal sealed class BridgeOptions
{
    public const string SectionName = "Bridge";

    /// <summary>The remote's ID, decimal or <c>0x</c> hex in the configuration.</summary>
    public uint? RemoteId { get; init; }

    /// <summary>
    ///     The IP addresses allowed to send commands, as text: the configuration binder can't bind
    ///     <see cref="IPAddress" />.
    /// </summary>
    public IReadOnlyList<string> AllowedClients { get; init; } = [];
}
