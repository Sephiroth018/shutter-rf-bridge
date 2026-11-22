using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ShutterRfBridge.Configuration;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Tests.TestSupport;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Tests.Transmission;

public sealed class TransmitWorkerTests : IAsyncDisposable
{
    // Only a safety net against a hanging test; the tests wait for the transmitter's calls, not for time.
    private static readonly TimeSpan TransmitTimeout = TimeSpan.FromSeconds(10);

    private static readonly ShutterMove PartialMove = new(3, RemoteCommand.Down, 500, TimeSpan.FromSeconds(10));

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly ILogger<TransmitWorker> _logger = Substitute.For<ILogger<TransmitWorker>>();
    private readonly TransmitQueue _queue;
    private readonly TimeProviderSubstitute _time = new();
    private readonly TaskCompletionSource<RemoteFrame> _transmitted = new();
    private readonly IRemoteTransmitter _transmitter = Substitute.For<IRemoteTransmitter>();
    private readonly TransmitWorker _worker;

    public TransmitWorkerTests()
    {
        _queue = new TransmitQueue(_time.TimeProvider, Substitute.For<ILogger<TransmitQueue>>());
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var options = Options.Create(new BridgeOptions { RemoteId = MadeUpRemote.Id, AllowedClients = ["::1"] });
        _worker = new TransmitWorker(_queue, _transmitter, options, _logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _worker.StopAsync(CancellationToken.None);
        _worker.Dispose();
        _queue.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_QueuedCommand_TransmitsItsFrame()
    {
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Any<RemoteFrame>()))
            .Do(call => _transmitted.SetResult(call.Arg<RemoteFrame>()));
        await _worker.StartAsync(_cancellationToken);

        _queue.Enqueue(new ShutterStop(3));

        Assert.Equal(new RemoteFrame(MadeUpRemote.Id, 3, RemoteCommand.Stop),
            await _transmitted.Task.WaitAsync(TransmitTimeout, _cancellationToken));
    }

    [Fact]
    public async Task ExecuteAsync_TransmitFails_LogsAnErrorAndSendsTheNextCommand()
    {
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Is<RemoteFrame>(frame => frame.Channel == 3)))
            .Throw(new TransmitException("The radio didn't answer."));
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Is<RemoteFrame>(frame => frame.Channel == 2)))
            .Do(call => _transmitted.SetResult(call.Arg<RemoteFrame>()));
        await _worker.StartAsync(_cancellationToken);

        _queue.Enqueue(new ShutterStop(3));
        _queue.Enqueue(new ShutterStop(2));

        await _transmitted.Task.WaitAsync(TransmitTimeout, _cancellationToken);
        _logger.Received(1).Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(),
            Arg.Any<TransmitException>(), Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_FaultsTheWorker()
    {
        var execution = await StartFailingWorker();

        _queue.Enqueue(new ShutterStop(3));

        await Assert.ThrowsAsync<ArgumentException>(() => execution.WaitAsync(TransmitTimeout, _cancellationToken));
    }

    [Fact]
    public async Task StoppingAsync_PendingTimedStop_Waits()
    {
        await StartMovingPartially();

        var stopping = _worker.StoppingAsync(_cancellationToken);

        Assert.False(stopping.IsCompleted);
    }

    [Fact]
    public async Task StoppingAsync_PendingTimedStop_SendsItBeforeTheWorkerStops()
    {
        await StartMovingPartially();
        var stopping = _worker.StoppingAsync(_cancellationToken);

        _time.Timers[0].Fire();
        await stopping.WaitAsync(TransmitTimeout, _cancellationToken);
        await _worker.StopAsync(_cancellationToken);

        _transmitter.Received(1).Transmit(new RemoteFrame(MadeUpRemote.Id, PartialMove.Channel, RemoteCommand.Stop));
    }

    [Fact]
    public async Task StoppingAsync_ShutdownTimeoutEnds_LogsAWarning()
    {
        await StartMovingPartially();
        using var shutdownTimeout = new CancellationTokenSource();
        var stopping = _worker.StoppingAsync(shutdownTimeout.Token);

        await shutdownTimeout.CancelAsync();
        await stopping.WaitAsync(TransmitTimeout, _cancellationToken);

        ReceivedWarning();
    }

    [Fact]
    public async Task StoppingAsync_WorkerFailed_ReturnsWithAWarning()
    {
        var execution = await StartFailingWorker();
        _queue.Enqueue(PartialMove);
        await Assert.ThrowsAsync<ArgumentException>(() => execution.WaitAsync(TransmitTimeout, _cancellationToken));

        await _worker.StoppingAsync(_cancellationToken).WaitAsync(TransmitTimeout, _cancellationToken);

        ReceivedWarning();
    }

    [Fact]
    public async Task StoppingAsync_WorkerFailed_LogsNoWait()
    {
        var execution = await StartFailingWorker();
        _queue.Enqueue(PartialMove);
        await Assert.ThrowsAsync<ArgumentException>(() => execution.WaitAsync(TransmitTimeout, _cancellationToken));

        await _worker.StoppingAsync(_cancellationToken).WaitAsync(TransmitTimeout, _cancellationToken);

        // Read from the received calls: an assertion call to Log at Information trips CA1873.
        Assert.DoesNotContain(_logger.ReceivedCalls(), call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments() is [LogLevel.Information, ..]);
    }

    /// <summary>Starts the worker with a transmitter that throws a bug's exception; returns the worker's loop.</summary>
    private async Task<Task> StartFailingWorker()
    {
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Any<RemoteFrame>()))
            .Throw(new ArgumentException("A bug."));
        await _worker.StartAsync(_cancellationToken);
        var execution = _worker.ExecuteTask;
        Assert.NotNull(execution);
        return execution;
    }

    /// <summary>Starts the worker and returns once it has sent a partial move, whose timed Stop is then pending.</summary>
    private async Task StartMovingPartially()
    {
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Any<RemoteFrame>()))
            .Do(call => _transmitted.TrySetResult(call.Arg<RemoteFrame>()));
        await _worker.StartAsync(_cancellationToken);
        _queue.Enqueue(PartialMove);
        await _transmitted.Task.WaitAsync(TransmitTimeout, _cancellationToken);
    }

    private void ReceivedWarning() =>
        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), Arg.Any<Exception?>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
}
