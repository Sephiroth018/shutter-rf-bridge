using ShutterRfBridge.Remote;
using ShutterRfBridge.Transmission;

namespace ShutterRfBridge.Tests.Transmission;

public sealed class ShutterMoveTests
{
    [Theory]
    [InlineData(ShutterMove.MinLevel, false)]
    [InlineData(1, true)]
    [InlineData(999, true)]
    [InlineData(ShutterMove.MaxLevel, false)]
    public void IsPartial_Level_IsTrueOnlyBetweenTheEndPositions(int level, bool isPartial)
    {
        var move = new ShutterMove(3, RemoteCommand.Up, level, TimeSpan.FromSeconds(10));

        Assert.Equal(isPartial, move.IsPartial);
    }
}
