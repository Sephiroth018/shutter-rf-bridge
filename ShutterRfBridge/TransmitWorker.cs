using Microsoft.Extensions.Options;

namespace ShutterRfBridge;

/// <summary>Sends the <see cref="TransmitQueue" />'s commands one at a time.</summary>
internal sealed partial class TransmitWorker : BackgroundService
{
    private readonly ILogger<TransmitWorker> _logger;
    private readonly TransmitQueue _queue;
    private readonly uint _remoteId;
    private readonly IRemoteTransmitter _transmitter;

    public TransmitWorker(
        TransmitQueue queue,
        IRemoteTransmitter transmitter,
        IOptions<BridgeOptions> options,
        ILogger<TransmitWorker> logger)
    {
        _queue = queue;
        _transmitter = transmitter;
        // BridgeOptionsValidator has ensured the ID at startup.
        _remoteId = options.Value.RemoteId!.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            var command = await _queue.DequeueAsync(stoppingToken);
            try
            {
                _transmitter.Transmit(new RemoteFrame(_remoteId, command.Channel, command.Command));
                LogSent(command.Command, command.Channel);
            }
            // The next command starts the radio afresh, so the worker goes on; any other exception stops the service.
            catch (TransmitException exception)
            {
                LogFailed(exception, command.Command, command.Channel);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Command} on channel {Channel}")]
    private partial void LogSent(RemoteCommand command, int channel);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send {Command} on channel {Channel}")]
    private partial void LogFailed(Exception exception, RemoteCommand command, int channel);
}
