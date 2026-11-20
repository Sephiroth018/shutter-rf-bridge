namespace ShutterRfBridge.Remote;

/// <summary>
///     Encodes a <see cref="RemoteFrame" /> as one fixed-length CC1101 packet. Each packet bit is one time unit of
///     ~119 µs, sent as carrier on (1) or off (0); the CC1101 sends each byte MSB first.
/// </summary>
internal static class RemotePacketEncoder
{
    private const int SyncHighUnits = 41;
    private const int SyncLowUnits = 12;
    private const int ShortPulseUnits = 3;
    private const int LongPulseUnits = 6;
    private const int GapUnits = 60;

    private const int BitUnits = ShortPulseUnits + LongPulseUnits;

    // The gap replaces the last bit's low; the longest frame ends in a 1, whose low is the short one.
    private const int MaxUnitCount =
        SyncHighUnits + SyncLowUnits + RemoteFrame.BitCount * BitUnits - ShortPulseUnits + GapUnits;

    /// <summary>The packet length in bytes, the same for every frame.</summary>
    public const int PacketLength = (MaxUnitCount + 7) / 8;

    /// <summary>
    ///     Encodes the sync, then each bit as a high and a low (1: long high, short low; 0: short high, long low),
    ///     with the gap in place of the last bit's low, zero-padded to <see cref="PacketLength" />.
    /// </summary>
    public static ReadOnlyMemory<byte> Encode(RemoteFrame frame)
    {
        // The packet starts as zeros, so only the highs are set: the lows, the gap and the padding stay zero.
        var packet = new byte[PacketLength];
        SetHigh(packet, 0, SyncHighUnits);
        var bits = frame.Bits;
        for (var bitIndex = 0; bitIndex < RemoteFrame.BitCount; bitIndex++)
        {
            var isOne = ((bits >> (RemoteFrame.BitCount - 1 - bitIndex)) & 1) == 1;
            var start = SyncHighUnits + SyncLowUnits + bitIndex * BitUnits;
            SetHigh(packet, start, isOne ? LongPulseUnits : ShortPulseUnits);
        }

        return packet;
    }

    private static void SetHigh(byte[] packet, int start, int units)
    {
        for (var position = start; position < start + units; position++)
        {
            packet[position / 8] |= (byte)(0x80 >> (position % 8));
        }
    }
}
