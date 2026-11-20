namespace ShutterRfBridge;

/// <summary>Sends a remote's command to the shutters.</summary>
internal interface IRemoteTransmitter
{
    /// <summary>Sends the command in <paramref name="frame" />, returning once it has been sent.</summary>
    void Transmit(RemoteFrame frame);
}
