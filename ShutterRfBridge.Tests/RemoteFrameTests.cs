using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests;

public sealed class RemoteFrameTests
{
    [Fact]
    public void Constructor_RemoteIdWiderThan28Bits_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteFrame(0x1000_0000, 3, RemoteCommand.Up));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void Constructor_ChannelOutside0To15_ThrowsArgumentOutOfRange(int channel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteFrame(MadeUpRemote.Id, channel, RemoteCommand.Up));
    }

    [Fact]
    public void Constructor_UndefinedCommand_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RemoteFrame(MadeUpRemote.Id, 3, (RemoteCommand) 0x99));
    }

    [Theory]
    [InlineData(0u, 0, 0x00_0000_0011UL)]
    [InlineData(0x0FFF_FFFFu, 15, 0xFF_FFFF_FF11UL)]
    public void Bits_SmallestAndLargestIdAndChannel_ComposesThem(uint remoteId, int channel, ulong expectedBits)
    {
        var frame = new RemoteFrame(remoteId, channel, RemoteCommand.Up);

        Assert.Equal(expectedBits, frame.Bits);
    }

    // Test methods are public, so they take the internal RemoteCommand as its code.
    [Theory]
    [InlineData((byte) RemoteCommand.Up, 0xA5_C3E7_1311UL)]
    [InlineData((byte) RemoteCommand.Down, 0xA5_C3E7_1333UL)]
    [InlineData((byte) RemoteCommand.Stop, 0xA5_C3E7_1355UL)]
    public void Bits_MadeUpRemoteOnChannel3_ComposesIdChannelAndCommand(byte commandCode, ulong expectedBits)
    {
        var frame = new RemoteFrame(MadeUpRemote.Id, 3, (RemoteCommand) commandCode);

        Assert.Equal(expectedBits, frame.Bits);
    }
}
