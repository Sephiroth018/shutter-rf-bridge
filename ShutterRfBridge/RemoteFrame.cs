namespace ShutterRfBridge;

/// <summary>One frame the remote sends: its ID, the shutter channel and the command.</summary>
internal sealed record RemoteFrame
{
    private const int RemoteIdBitCount = 28;
    private const int ChannelBitCount = 4;
    private const int CommandBitCount = 8;
    private const uint MaxRemoteId = (1u << RemoteIdBitCount) - 1;
    private const int MaxChannel = (1 << ChannelBitCount) - 1;

    public const int BitCount = RemoteIdBitCount + ChannelBitCount + CommandBitCount;

    public RemoteFrame(uint remoteId, int channel, RemoteCommand command)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(remoteId, MaxRemoteId);
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(channel, MaxChannel);
        if (!Enum.IsDefined(command))
        {
            throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown remote command.");
        }

        RemoteId = remoteId;
        Channel = channel;
        Command = command;
    }

    public uint RemoteId { get; }

    public int Channel { get; }

    public RemoteCommand Command { get; }

    /// <summary>
    ///     The frame's <see cref="BitCount" /> bits in sending order, MSB first: remote ID, channel, command.
    /// </summary>
    public ulong Bits =>
        ((ulong)RemoteId << (ChannelBitCount + CommandBitCount)) |
        ((ulong)Channel << CommandBitCount) |
        (byte)Command;
}
