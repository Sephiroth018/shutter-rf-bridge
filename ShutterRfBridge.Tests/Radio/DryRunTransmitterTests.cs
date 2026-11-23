using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Radio;

public sealed class DryRunTransmitterTests
{
    [Fact]
    public void Transmit_AnyFrame_LogsTheCommandAndChannelAtInformation()
    {
        var logger = new FakeLogger<DryRunTransmitter>();
        var transmitter = new DryRunTransmitter(logger);
        var frame = new RemoteFrame(MadeUpRemote.Id, 3, RemoteCommand.Down);

        transmitter.Transmit(frame);

        var log = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal($"Dry run: {frame.Command} on channel {frame.Channel} not transmitted", log.Message);
    }
}
