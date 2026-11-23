namespace ShutterRfBridge.Weather;

/// <summary>No current rain is known; <see cref="Reason" /> says why, for the log.</summary>
internal sealed record RainUnknown(string Reason) : RainReport
{
    public const string Answer = "unknown";

    public override RainCondition Condition => RainCondition.Unknown;

    public override string ToAnswer() => Answer;
}
