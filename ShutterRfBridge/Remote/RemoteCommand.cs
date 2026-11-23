namespace ShutterRfBridge.Remote;

/// <summary>The command codes the remote sends as a frame's last byte.</summary>
/// <remarks>Public because the public <see cref="Transmission.ShutterCommand.Command" /> exposes it.</remarks>
public enum RemoteCommand : byte
{
    Up = 0x11,
    Down = 0x33,
    Stop = 0x55
}
