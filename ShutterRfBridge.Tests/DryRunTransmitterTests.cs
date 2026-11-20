using Microsoft.Extensions.Logging;
using NSubstitute;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests;

public sealed class DryRunTransmitterTests
{
    [Fact]
    public void Transmit_AnyFrame_LogsTheCommandAndChannelAtInformation()
    {
        // ILogger<DryRunTransmitter> can't be substituted for an internal type, so the real Logger<T> wraps a substitute.
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        var transmitter = new DryRunTransmitter(new Logger<DryRunTransmitter>(loggerFactory));

        transmitter.Transmit(new RemoteFrame(MadeUpRemote.Id, 3, RemoteCommand.Down));

        var log = Assert.Single(logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
        var arguments = log.GetArguments();
        Assert.Equal(LogLevel.Information, arguments[0]);
        Assert.Equal("Dry run: Down on channel 3 not transmitted", arguments[2]?.ToString());
    }
}
