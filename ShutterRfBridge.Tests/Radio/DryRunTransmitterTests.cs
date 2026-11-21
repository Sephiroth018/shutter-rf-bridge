using Microsoft.Extensions.Logging;
using NSubstitute;
using ShutterRfBridge.Radio;
using ShutterRfBridge.Remote;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Radio;

public sealed class DryRunTransmitterTests
{
    [Fact]
    public void Transmit_AnyFrame_LogsTheCommandAndChannelAtInformation()
    {
        var logger = Substitute.For<ILogger<DryRunTransmitter>>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        var transmitter = new DryRunTransmitter(logger);
        var frame = new RemoteFrame(MadeUpRemote.Id, 3, RemoteCommand.Down);

        transmitter.Transmit(frame);

        var log = Assert.Single(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
        var arguments = log.GetArguments();
        Assert.Equal(LogLevel.Information, arguments[0]);
        Assert.Equal($"Dry run: {frame.Command} on channel {frame.Channel} not transmitted", arguments[2]?.ToString());
    }
}
