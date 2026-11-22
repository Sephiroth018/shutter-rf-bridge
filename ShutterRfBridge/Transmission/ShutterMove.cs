using ShutterRfBridge.Remote;

namespace ShutterRfBridge.Transmission;

/// <summary>Moves a shutter towards a level; <see cref="RunTime" /> is how long the CCU expects the move to take.</summary>
internal sealed record ShutterMove(int Channel, RemoteCommand Command, int Level, TimeSpan RunTime)
    : ShutterCommand(Channel, Command)
{
    public const int MinLevel = 0;
    public const int MaxLevel = 1000;

    /// <summary>The longest run time CUxD can send: an unsigned 16-bit number of milliseconds.</summary>
    public static readonly TimeSpan MaxRunTime = TimeSpan.FromMilliseconds(ushort.MaxValue);

    /// <summary>
    ///     Whether the move ends between the end positions, so a Stop has to end it. At an end position the motor
    ///     stops by itself.
    /// </summary>
    public bool IsPartial => Level is > MinLevel and < MaxLevel;
}
