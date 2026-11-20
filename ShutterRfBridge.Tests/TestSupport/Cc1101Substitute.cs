using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShutterRfBridge.Radio;

namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>
///     Builds a substituted SPI bus that answers like a CC1101, and a clock that stands still. Its constants are the
///     CC1101's wire bytes, restated rather than shared with the driver so the tests check them independently.
/// </summary>
internal sealed class Cc1101Substitute
{
    public const byte ResetStrobe = 0x30;
    public const byte CalibrateStrobe = 0x33;
    public const byte TransmitStrobe = 0x35;
    public const byte IdleStrobe = 0x36;
    public const byte FlushTxFifoStrobe = 0x3B;
    public const byte PaTableBurstWrite = 0x7E;
    public const byte TxFifoBurstWrite = 0x7F;
    public const byte VersionRead = 0xF1;
    public const byte MainStateRead = 0xF5;

    // The status byte answering a read's header.
    public const byte Ready = 0x00;
    public const byte NotReady = 0x80;

    public const byte Idle = 0x01;
    public const byte Transmitting = 0x13;
    public const byte TxUnderflow = 0x16;

    private readonly ISpiBus _bus = Substitute.For<ISpiBus>();
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    public Cc1101Substitute()
    {
        _clock.TimestampFrequency.Returns(TimeSpan.TicksPerSecond);
        WithVersion(0x14);
        WithMainState(Idle);
    }

    /// <summary>Every transfer's written bytes, in order.</summary>
    public IReadOnlyList<byte[]> Transfers =>
        _bus.ReceivedCalls().Select(call => call.GetArguments() is [byte[] write]
            ? write
            : throw new InvalidOperationException("Every SPI transfer takes one byte array.")).ToList();

    public Cc1101Substitute WithVersion(byte version)
    {
        _bus.Transfer(Arg.Is<byte[]>(write => write[0] == VersionRead)).Returns([Ready, version]);
        return this;
    }

    /// <summary>Answers main state reads with these status and state pairs in turn, repeating the last one.</summary>
    public Cc1101Substitute WithMainStateReads(byte[] first, params byte[][] rest)
    {
        _bus.Transfer(Arg.Is<byte[]>(write => write[0] == MainStateRead)).Returns(first, rest);
        return this;
    }

    /// <summary>Answers every main state read with this state.</summary>
    public Cc1101Substitute WithMainState(byte state) => WithMainStateReads([Ready, state]);

    /// <summary>Makes every transfer fail the way the SPI driver does.</summary>
    public Cc1101Substitute WithFailingTransfers()
    {
        _bus.Transfer(Arg.Any<byte[]>()).Throws(new IOException("The SPI transfer failed."));
        return this;
    }

    /// <summary>Makes the clock jump past every timeout after its first reading.</summary>
    public Cc1101Substitute WithClockRunningOut()
    {
        _clock.GetTimestamp().Returns(0, TimeSpan.TicksPerHour);
        return this;
    }

    public Cc1101Transmitter BuildTransmitter() => new(_bus, _clock);
}
