using Microsoft.Extensions.Options;
using ShutterRfBridge.Configuration;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Transmission;

/// <summary>
///     Sends the <see cref="TransmitQueue" />'s commands one at a time. When the host stops, it first sends the pending
///     commands, timed Stops included, so no shutter in a partial move runs to its end.
/// </summary>
internal sealed partial class TransmitWorker : BackgroundService, IHostedLifecycleService
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

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    ///     Waits for the queue's pending commands. The host calls this before stopping any service, so the HTTP API still
    ///     takes commands meanwhile; the token is cancelled when the host's shutdown timeout ends.
    /// </summary>
    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        if (ExecuteTask is null)
        {
            return;
        }

        var nothingPending = _queue.WaitForPendingCommandsAsync(cancellationToken);
        if (!nothingPending.IsCompleted)
        {
            LogWaitingForPendingCommands();
            // A failed worker sends nothing more, so waiting for the queue would only run into the timeout.
            await Task.WhenAny(nothingPending, ExecuteTask);
        }

        // The command taken last may still be on air; stopping the worker waits for that, as it ends the loop.
        if (!nothingPending.IsCompletedSuccessfully)
        {
            LogStoppedWithPendingCommands();
        }
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping: sending the pending commands first")]
    private partial void LogWaitingForPendingCommands();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stopped with commands still pending")]
    private partial void LogStoppedWithPendingCommands();
}
