namespace ShutterRfBridge.Weather;

/// <summary>No current rain is known; <see cref="Reason" /> says why, for the log.</summary>
public record RainUnknown(string Reason) : RainReport
{
    public override RainCondition Condition => RainCondition.Unknown;

    public override string ToAnswer() => "unknown";
}
