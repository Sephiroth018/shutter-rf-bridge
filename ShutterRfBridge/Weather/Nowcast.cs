namespace ShutterRfBridge.Weather;

/// <summary>
///     One run of the rain nowcast for one point: the run's reference time and the step asked for, or
///     <see langword="null" /> when the run doesn't cover it.
/// </summary>
public record Nowcast(DateTimeOffset ReferenceTime, NowcastStep? Step)
{
    /// <summary>The nowcast's time resolution.</summary>
    public static readonly TimeSpan StepLength = TimeSpan.FromMinutes(15);

    /// <summary>The time of the step <paramref name="time" /> falls in: rounded down to <see cref="StepLength" />, in UTC.</summary>
    public static DateTimeOffset StepTimeAt(DateTimeOffset time) =>
        new(time.UtcTicks - time.UtcTicks % StepLength.Ticks, TimeSpan.Zero);
}
