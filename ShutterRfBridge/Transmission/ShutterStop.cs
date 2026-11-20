using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Transmission;

/// <summary>Stops a shutter.</summary>
internal sealed record ShutterStop(int Channel) : ShutterCommand(Channel, RemoteCommand.Stop);
