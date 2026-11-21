using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Transmission;

/// <summary>A command for one shutter channel, as the CCU sends it.</summary>
public abstract record ShutterCommand(int Channel, RemoteCommand Command);
