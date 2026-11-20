using Microsoft.Extensions.Logging;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests;

public sealed class TransmitQueueTests : IDisposable
{
    private static readonly TimeSpan RunTime = TimeSpan.FromSeconds(10);

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly TransmitQueue _queue;
    private readonly TimeProviderSubstitute _time = new();

    public TransmitQueueTests()
    {
        _queue = new TransmitQueue(_time.TimeProvider, Substitute.For<ILogger<TransmitQueue>>());
    }

    public void Dispose()
    {
        _queue.Dispose();
    }

    [Fact]
    public async Task DequeueAsync_TwoMoves_ReturnsThemInArrivalOrder()
    {
        var first = PartialMove(3);
        var second = PartialMove(2);
        _queue.Enqueue(first);
        _queue.Enqueue(second);

        var dequeued = new[] { await Dequeue(), await Dequeue() };

        Assert.Equal([first, second], dequeued);
    }

    [Fact]
    public async Task DequeueAsync_StopBehindMove_ReturnsStopFirst()
    {
        var stop = new ShutterStop(2);
        _queue.Enqueue(PartialMove(3));
        _queue.Enqueue(stop);

        var dequeued = await Dequeue();

        Assert.Equal(stop, dequeued);
    }

    [Fact]
    public async Task DequeueAsync_TwoStopsBehindMove_ReturnsStopsInArrivalOrder()
    {
        var first = new ShutterStop(2);
        var second = new ShutterStop(1);
        _queue.Enqueue(PartialMove(3));
        _queue.Enqueue(first);
        _queue.Enqueue(second);

        var dequeued = new[] { await Dequeue(), await Dequeue() };

        Assert.Equal<ShutterCommand>([first, second], dequeued);
    }

    [Fact]
    public async Task DequeueAsync_NothingQueued_Waits()
    {
        var dequeue = _queue.DequeueAsync(_cancellationToken);

        Assert.False(dequeue.IsCompleted);
        _queue.Enqueue(new ShutterStop(3));
        await dequeue;
    }

    [Fact]
    public async Task Enqueue_Stop_RemovesTheChannelsQueuedCommands()
    {
        var otherMove = PartialMove(2);
        var stop = new ShutterStop(3);
        _queue.Enqueue(PartialMove(3));
        _queue.Enqueue(otherMove);
        _queue.Enqueue(stop);

        var dequeued = new[] { await Dequeue(), await Dequeue() };

        Assert.Equal([stop, otherMove], dequeued);
        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task DequeueAsync_Move_StartsATimerForItsRunTime()
    {
        _queue.Enqueue(PartialMove(3));

        await Dequeue();

        Assert.Equal(RunTime, Assert.Single(_time.Timers).DueTime);
    }

    [Fact]
    public async Task DequeueAsync_PartialMoveRunTimeEnded_ReturnsAStop()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();

        _time.Timers[0].Fire();

        Assert.Equal(new ShutterStop(3), await Dequeue());
    }

    [Theory]
    [InlineData(ShutterMove.MinLevel)]
    [InlineData(ShutterMove.MaxLevel)]
    public async Task DequeueAsync_FullMoveRunTimeEnded_ReturnsNothing(int level)
    {
        _queue.Enqueue(new ShutterMove(3, RemoteCommand.Up, level, RunTime));
        await Dequeue();

        _time.Timers[0].Fire();

        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task Enqueue_CommandForMovingChannel_CancelsItsTimedStop()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();
        var newMove = PartialMove(3);

        _queue.Enqueue(newMove);
        _time.Timers[0].Fire();

        _time.Timers[0].Timer.Received().Dispose();
        Assert.Equal(newMove, await Dequeue());
        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task Enqueue_MoveForMovingChannel_GoesAheadOfQueuedMoves()
    {
        _queue.Enqueue(new ShutterMove(3, RemoteCommand.Down, ShutterMove.MaxLevel, RunTime));
        await Dequeue();
        _queue.Enqueue(PartialMove(2));
        var correction = PartialMove(3);

        _queue.Enqueue(correction);

        Assert.Equal(correction, await Dequeue());
    }

    [Fact]
    public async Task Enqueue_MoveForMovingChannel_KeepsTheChannelsOlderQueuedMovesAheadOfIt()
    {
        _queue.Enqueue(PartialMove(3));
        var olderMove = new ShutterMove(3, RemoteCommand.Down, 200, RunTime);
        _queue.Enqueue(olderMove);
        _queue.Enqueue(PartialMove(2));
        await Dequeue();
        var correction = PartialMove(3);

        _queue.Enqueue(correction);

        var dequeued = new[] { await Dequeue(), await Dequeue() };
        Assert.Equal([olderMove, correction], dequeued);
    }

    [Fact]
    public async Task DequeueAsync_SecondMoveForChannel_CancelsTheFirstMovesTimedStop()
    {
        _queue.Enqueue(PartialMove(3));
        _queue.Enqueue(PartialMove(3));
        await Dequeue();

        await Dequeue();
        _time.Timers[0].Fire();

        _time.Timers[0].Timer.Received().Dispose();
        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    private static ShutterMove PartialMove(int channel) => new(channel, RemoteCommand.Up, 500, RunTime);

    private Task<ShutterCommand> Dequeue() => _queue.DequeueAsync(_cancellationToken);
}
