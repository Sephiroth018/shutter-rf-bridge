using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
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

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly FakeLogger<TransmitWorker> _logger = new();
    private readonly TransmitQueue _queue = new(new FakeTimeProvider(), NullLogger<TransmitQueue>.Instance);
    private readonly TaskCompletionSource<RemoteFrame> _transmitted = new();
    private readonly IRemoteTransmitter _transmitter = Substitute.For<IRemoteTransmitter>();
    private readonly TransmitWorker _worker;

    public TransmitWorkerTests()
    {
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
        var error = Assert.Single(_logger.Collector.GetSnapshot(), log => log.Level == LogLevel.Error);
        Assert.IsType<TransmitException>(error.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_FaultsTheWorker()
    {
        _transmitter.When(transmitter => transmitter.Transmit(Arg.Any<RemoteFrame>()))
            .Throw(new ArgumentException("A bug."));
        await _worker.StartAsync(_cancellationToken);

        var execution = _worker.ExecuteTask;
        Assert.NotNull(execution);

        _queue.Enqueue(new ShutterStop(3));

        await Assert.ThrowsAsync<ArgumentException>(() => execution.WaitAsync(TransmitTimeout, _cancellationToken));
    }
}
