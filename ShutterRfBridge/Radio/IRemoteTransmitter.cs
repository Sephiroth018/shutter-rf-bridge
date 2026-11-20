using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Radio;

/// <summary>Sends a remote's command to the shutters.</summary>
internal interface IRemoteTransmitter
{
    /// <summary>Sends the command in <paramref name="frame" />, returning once it has been sent.</summary>
    /// <exception cref="TransmitException">The radio failed; the next command starts afresh.</exception>
    void Transmit(RemoteFrame frame);
}
