namespace ShutterRfBridge.Weather;

/// <summary>One run of the rain nowcast for one point: the run's reference time and the steps asked for.</summary>
internal sealed record Nowcast(DateTimeOffset ReferenceTime, IReadOnlyList<NowcastStep> Steps)
{
    /// <summary>The nowcast's time resolution.</summary>
    public static readonly TimeSpan StepLength = TimeSpan.FromMinutes(15);

    /// <summary>The time of the step <paramref name="time" /> falls in: rounded down to <see cref="StepLength" />, in UTC.</summary>
    public static DateTimeOffset StepTimeAt(DateTimeOffset time) =>
        new(time.UtcTicks - time.UtcTicks % StepLength.Ticks, TimeSpan.Zero);
}
