namespace ShutterRfBridge.Radio;

/// <summary>A command couldn't be sent because of the radio, such as a wiring fault or a radio that stopped answering.</summary>
internal sealed class TransmitException : Exception
{
    public TransmitException(string message) : base(message)
    {
    }

    public TransmitException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
