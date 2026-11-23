using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Tests.Transmission;

public sealed class TransmitQueueTests : IDisposable
{
    private static readonly TimeSpan RunTime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan JustBeforeRunTime = RunTime - TimeSpan.FromMilliseconds(1);

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly TransmitQueue _queue;
    private readonly FakeTimeProvider _time = new();

    public TransmitQueueTests()
    {
        _queue = new TransmitQueue(_time, NullLogger<TransmitQueue>.Instance);
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
    public async Task DequeueAsync_PartialMoveRunTimeNotEnded_ReturnsNothing()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();

        _time.Advance(JustBeforeRunTime);

        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task DequeueAsync_PartialMoveRunTimeEnded_ReturnsAStop()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();

        _time.Advance(RunTime);

        Assert.Equal(new ShutterStop(3), await Dequeue());
    }

    [Fact]
    public async Task DequeueAsync_PartialMoveRunTimeLongEnded_ReturnsOnlyOneStop()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();
        _time.Advance(RunTime);
        await Dequeue();

        _time.Advance(RunTime);

        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Theory]
    [InlineData(ShutterMove.MinLevel)]
    [InlineData(ShutterMove.MaxLevel)]
    public async Task DequeueAsync_FullMoveRunTimeEnded_ReturnsNothing(int level)
    {
        _queue.Enqueue(new ShutterMove(3, RemoteCommand.Up, level, RunTime));
        await Dequeue();

        _time.Advance(RunTime);

        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task Enqueue_CommandForMovingChannel_CancelsItsTimedStop()
    {
        _queue.Enqueue(PartialMove(3));
        await Dequeue();
        var newMove = PartialMove(3);

        _queue.Enqueue(newMove);
        _time.Advance(RunTime);

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
        var halfRunTime = RunTime / 2;
        _queue.Enqueue(PartialMove(3));
        _queue.Enqueue(PartialMove(3));
        await Dequeue();
        _time.Advance(halfRunTime);

        await Dequeue();
        // The first move's run time is over, the second's isn't.
        _time.Advance(halfRunTime);

        Assert.False(_queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    [Fact]
    public async Task Enqueue_CommandForMovingChannelBeforeItsRunTimeEndRuns_KeepsTheCommand()
    {
        using var queue = QueueWithInFlightRunTimeEnds(out var runTimeEnds);
        queue.Enqueue(PartialMove(3));
        await queue.DequeueAsync(_cancellationToken);
        var newMove = PartialMove(3);
        queue.Enqueue(newMove);

        runTimeEnds[0]();

        Assert.Equal(newMove, await queue.DequeueAsync(_cancellationToken));
    }

    [Fact]
    public async Task DequeueAsync_SecondMoveForChannelBeforeTheFirstsRunTimeEndRuns_KeepsTheChannelMoving()
    {
        using var queue = QueueWithInFlightRunTimeEnds(out var runTimeEnds);
        queue.Enqueue(PartialMove(3));
        queue.Enqueue(PartialMove(3));
        await queue.DequeueAsync(_cancellationToken);
        await queue.DequeueAsync(_cancellationToken);

        runTimeEnds[0]();

        Assert.False(queue.DequeueAsync(_cancellationToken).IsCompleted);
    }

    private static ShutterMove PartialMove(int channel) => new(channel, RemoteCommand.Up, 500, RunTime);

    /// <summary>
    ///     A queue whose run time ends a test runs by hand from <paramref name="runTimeEnds" />, also after their timer
    ///     was disposed: <see cref="FakeTimeProvider" /> never fires a disposed timer, so it can't replay a callback that
    ///     was already running when a newer command ended the movement.
    /// </summary>
    private static TransmitQueue QueueWithInFlightRunTimeEnds(out IReadOnlyList<Action> runTimeEnds)
    {
        var ends = new List<Action>();
        runTimeEnds = ends;
        var clock = Substitute.For<TimeProvider>();
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(call =>
            {
                var callback = call.ArgAt<TimerCallback>(0);
                var state = call.ArgAt<object?>(1);
                ends.Add(() => callback(state));
                return Substitute.For<ITimer>();
            });
        return new TransmitQueue(clock, NullLogger<TransmitQueue>.Instance);
    }

    private Task<ShutterCommand> Dequeue() => _queue.DequeueAsync(_cancellationToken);
}
