namespace ShutterRfBridge;

/// <summary>The command codes the remote sends as a frame's last byte.</summary>
internal enum RemoteCommand : byte
{
    Up = 0x11,
    Down = 0x33,
    Stop = 0x55
}
