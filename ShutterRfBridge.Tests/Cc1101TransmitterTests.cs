using ShutterRfBridge.Tests.TestSupport;
using static ShutterRfBridge.Tests.TestSupport.Cc1101Substitute;

namespace ShutterRfBridge.Tests;

public sealed class Cc1101TransmitterTests
{
    private static readonly RemoteFrame Frame = new(MadeUpRemote.Id, 3, RemoteCommand.Up);

    [Fact]
    public void Transmit_HealthyRadio_ResetsChecksVersionWritesSettingsAndCalibratesBeforeTheFirstPacket()
    {
        var radio = new Cc1101Substitute();

        radio.BuildTransmitter().Transmit(Frame);

        byte[][] expected =
        [
            [ResetStrobe],
            [VersionRead, 0x00],
            [0x02, 0x2E], [0x06, 59], [0x07, 0x00], [0x08, 0x00], [0x0D, 0x10], [0x0E, 0xB0], [0x0F, 0x0D],
            [0x10, 0x88], [0x11, 0x53], [0x12, 0x30], [0x13, 0x02], [0x17, 0x00], [0x18, 0x08], [0x22, 0x11],
            [0x23, 0xE9], [0x24, 0x2A], [0x25, 0x00], [0x26, 0x1F], [0x2C, 0x81], [0x2D, 0x35], [0x2E, 0x09],
            [PaTableBurstWrite, 0x00, 0xC0],
            [CalibrateStrobe]
        ];
        var configuration = radio.Transfers.Where(write => write[0] != MainStateRead)
            .TakeWhile(write => write[0] != TxFifoBurstWrite);
        Assert.Equal(expected, configuration);
    }

    [Fact]
    public void Transmit_HealthyRadio_SendsThePacketSixTimesWaitingForIdleAfterEach()
    {
        var radio = new Cc1101Substitute();

        radio.BuildTransmitter().Transmit(Frame);

        byte[] mainStateRead = [MainStateRead, 0x00];
        byte[][] onePacket =
        [
            [TxFifoBurstWrite, .. RemotePacketEncoder.Encode(Frame).ToArray()],
            [TransmitStrobe],
            mainStateRead,
            mainStateRead
        ];
        var packets = radio.Transfers.SkipWhile(write => write[0] != TxFifoBurstWrite).SkipLast(2);
        Assert.Equal(Enumerable.Repeat(onePacket, 6).SelectMany(packet => packet), packets);
    }

    [Fact]
    public void Transmit_HealthyRadio_EndsWithIdleAndFlush()
    {
        var radio = new Cc1101Substitute();

        radio.BuildTransmitter().Transmit(Frame);

        Assert.Equal([[IdleStrobe], [FlushTxFifoStrobe]], radio.Transfers.TakeLast(2));
    }

    [Fact]
    public void Transmit_MainStateReadsDisagree_WaitsForTwoAgreeingReads()
    {
        var radio = new Cc1101Substitute().WithMainStateReads([Ready, Idle], [Ready, Transmitting], [Ready, Idle]);

        radio.BuildTransmitter().Transmit(Frame);

        Assert.Equal(4, MainStateReadsBeforeVersionRead(radio.Transfers));
    }

    [Fact]
    public void Transmit_ChipNotReadyAfterReset_IgnoresItsAnswersUntilReady()
    {
        var radio = new Cc1101Substitute().WithMainStateReads([NotReady, Idle], [NotReady, Idle], [Ready, Idle]);

        radio.BuildTransmitter().Transmit(Frame);

        Assert.Equal(4, MainStateReadsBeforeVersionRead(radio.Transfers));
    }

    [Fact]
    public void Transmit_WrongVersion_ThrowsTransmitException()
    {
        var radio = new Cc1101Substitute().WithVersion(0xFF);

        Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));
    }

    [Fact]
    public void Transmit_TxUnderflow_ThrowsTransmitException()
    {
        var radio = new Cc1101Substitute().WithMainState(TxUnderflow);

        Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));
    }

    [Fact]
    public void Transmit_RadioNeverReturnsToIdle_ThrowsTransmitException()
    {
        var radio = new Cc1101Substitute().WithMainState(Transmitting).WithClockRunningOut();

        Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));
    }

    [Fact]
    public void Transmit_ChipNeverReady_ThrowsTransmitException()
    {
        var radio = new Cc1101Substitute().WithMainStateReads([NotReady, Idle]).WithClockRunningOut();

        Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));
    }

    [Fact]
    public void Transmit_SpiTransferFails_ThrowsTransmitExceptionWithTheCause()
    {
        var radio = new Cc1101Substitute().WithFailingTransfers();

        var exception = Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));

        Assert.IsType<IOException>(exception.InnerException);
    }

    [Fact]
    public void Transmit_RadioNeverReturnsToIdle_EndsWithIdleAndFlush()
    {
        var radio = new Cc1101Substitute().WithMainState(Transmitting).WithClockRunningOut();

        Assert.Throws<TransmitException>(() => radio.BuildTransmitter().Transmit(Frame));

        Assert.Equal([[IdleStrobe], [FlushTxFifoStrobe]], radio.Transfers.TakeLast(2));
    }

    private static int MainStateReadsBeforeVersionRead(IEnumerable<byte[]> transfers) =>
        transfers.TakeWhile(write => write[0] != VersionRead).Count(write => write[0] == MainStateRead);
}
