namespace ShutterRfBridge;

/// <summary>The command codes the remote sends as a frame's last byte.</summary>
/// <remarks>Public so that xUnit's public test methods can take it as a theory parameter.</remarks>
public enum RemoteCommand : byte
{
    Up = 0x11,
    Down = 0x33,
    Stop = 0x55
}
