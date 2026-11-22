using System.Diagnostics.CodeAnalysis;
using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Transmission;

/// <summary>
///     The commands waiting to be sent, and the channels that are moving. Stops, and moves for a moving channel, go
///     ahead of the other moves, in the order they arrive. A move's run time starts when it is taken to be sent.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "It is a queue; the rule only expects collection types to end in Queue.")]
public partial class TransmitQueue : IDisposable
{
    private readonly SemaphoreSlim _commandsAdded = new(0);
    private readonly Lock _lock = new();
    private readonly ILogger<TransmitQueue> _logger;

    // Each moving channel's move, with the timer that ends the movement when the move's run time is over.
    private readonly Dictionary<int, (ShutterMove Move, ITimer Timer)> _movingChannels = [];
    private readonly List<ShutterCommand> _priorityCommands = [];
    private readonly List<ShutterCommand> _queuedMoves = [];
    private readonly TimeProvider _timeProvider;

    // Completed and cleared once nothing is pending; created only while something waits for it.
    private TaskCompletionSource? _nothingPending;

    public TransmitQueue(TimeProvider timeProvider, ILogger<TransmitQueue> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private bool HasPendingCommands =>
        _priorityCommands.Count > 0 || _queuedMoves.Count > 0 ||
        _movingChannels.Values.Any(movement => movement.Move.IsPartial);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        lock (_lock)
        {
            foreach (var (_, timer) in _movingChannels.Values)
            {
                timer.Dispose();
            }

            _movingChannels.Clear();
        }

        // _commandsAdded stays undisposed: a timer callback already past its lock may still release it, and a
        // SemaphoreSlim only holds unmanaged resources once its AvailableWaitHandle is used, which it never is here.
    }

    /// <summary>
    ///     Queues a received command. Any command ends its channel's movement; a Stop also removes the channel's queued
    ///     commands.
    /// </summary>
    public void Enqueue(ShutterCommand command)
    {
        lock (_lock)
        {
            var wasMoving = EndMovement(command.Channel);
            switch (command)
            {
                case ShutterMove move when wasMoving:
                    LogMoveReceived(move.Command, move.Channel, move.Level, (int)move.RunTime.TotalMilliseconds);
                    // The channel's older queued moves come along, so its commands are never sent out of order.
                    _priorityCommands.AddRange(_queuedMoves.Where(queued => queued.Channel == move.Channel));
                    _queuedMoves.RemoveAll(queued => queued.Channel == move.Channel);
                    _priorityCommands.Add(move);
                    break;
                case ShutterMove move:
                    LogMoveReceived(move.Command, move.Channel, move.Level, (int)move.RunTime.TotalMilliseconds);
                    _queuedMoves.Add(move);
                    break;
                case ShutterStop stop:
                    LogStopReceived(stop.Channel);
                    RemoveQueuedCommands(stop.Channel);
                    _priorityCommands.Add(stop);
                    break;
            }
        }

        _commandsAdded.Release();
    }

    /// <summary>
    ///     Waits for the next command to send and takes it. Taking a move starts its channel's movement, which lasts the
    ///     move's run time.
    /// </summary>
    public async Task<ShutterCommand> DequeueAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            // A Stop can remove queued commands, so a signal may find nothing left to take.
            await _commandsAdded.WaitAsync(cancellationToken);
            lock (_lock)
            {
                var queue = _priorityCommands.Count > 0 ? _priorityCommands : _queuedMoves;
                if (queue.Count == 0)
                {
                    continue;
                }

                var command = queue[0];
                queue.RemoveAt(0);
                EndMovement(command.Channel);
                if (command is ShutterMove move)
                {
                    StartMovement(move);
                }

                // Only taking a command can leave nothing pending: every other change queues a command.
                if (_nothingPending is not null && !HasPendingCommands)
                {
                    _nothingPending.SetResult();
                    _nothingPending = null;
                }

                return command;
            }
        }
    }

    /// <summary>
    ///     Waits until no command is queued and no partial move's timed Stop is pending. A full move owes nothing, so
    ///     its movement doesn't count.
    /// </summary>
    public Task WaitForPendingCommandsAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!HasPendingCommands)
            {
                return Task.CompletedTask;
            }

            _nothingPending ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _nothingPending.Task.WaitAsync(cancellationToken);
        }
    }

    private void StartMovement(ShutterMove move)
    {
        var timer = _timeProvider.CreateTimer(_ => EndRunTime(move), null, move.RunTime, Timeout.InfiniteTimeSpan);
        _movingChannels[move.Channel] = (move, timer);
    }

    private void EndRunTime(ShutterMove move)
    {
        lock (_lock)
        {
            // A newer command may have ended this movement while the callback waited for the lock. The same reference,
            // not an equal move: an identical later move is another movement.
            if (!_movingChannels.TryGetValue(move.Channel, out var movement) || !ReferenceEquals(movement.Move, move))
            {
                return;
            }

            EndMovement(move.Channel);
            if (!move.IsPartial)
            {
                return;
            }

            LogRunTimeEnded(move.Channel);
            RemoveQueuedCommands(move.Channel);
            _priorityCommands.Add(new ShutterStop(move.Channel));
        }

        _commandsAdded.Release();
    }

    /// <returns>Whether the channel was moving.</returns>
    private bool EndMovement(int channel)
    {
        if (!_movingChannels.Remove(channel, out var movement))
        {
            return false;
        }

        movement.Timer.Dispose();
        return true;
    }

    private void RemoveQueuedCommands(int channel)
    {
        _priorityCommands.RemoveAll(command => command.Channel == channel);
        _queuedMoves.RemoveAll(command => command.Channel == channel);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Received {Command} to level {Level} on channel {Channel}, run time {RunTimeMs} ms")]
    private partial void LogMoveReceived(RemoteCommand command, int channel, int level, int runTimeMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Received Stop on channel {Channel}")]
    private partial void LogStopReceived(int channel);

    [LoggerMessage(Level = LogLevel.Information, Message = "Run time ended on channel {Channel}, queueing a Stop")]
    private partial void LogRunTimeEnded(int channel);
}
