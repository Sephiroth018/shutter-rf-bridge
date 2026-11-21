using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Radio;

/// <summary>Logs each command instead of transmitting it, for development without the radio.</summary>
internal sealed partial class DryRunTransmitter : IRemoteTransmitter
{
    private readonly ILogger<DryRunTransmitter> _logger;

    public DryRunTransmitter(ILogger<DryRunTransmitter> logger)
    {
        _logger = logger;
    }

    public void Transmit(RemoteFrame frame)
    {
        LogNotTransmitted(frame.Command, frame.Channel);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Dry run: {Command} on channel {Channel} not transmitted")]
    private partial void LogNotTransmitted(RemoteCommand command, int channel);
}
