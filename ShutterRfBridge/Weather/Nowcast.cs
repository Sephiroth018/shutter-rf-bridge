namespace ShutterRfBridge.Weather;

/// <summary>
///     One run of the rain nowcast for one point: the run's reference time and, for each step asked for that the run
///     covers, in order, the millimetres per <see cref="StepLength" />, or <see langword="null" /> where it has no value.
/// </summary>
public record Nowcast(DateTimeOffset ReferenceTime, IReadOnlyList<double?> Millimetres)
{
    /// <summary>The nowcast's time resolution.</summary>
    public static readonly TimeSpan StepLength = TimeSpan.FromMinutes(15);

    /// <summary>The time of the step <paramref name="time" /> falls in: rounded down to <see cref="StepLength" />, in UTC.</summary>
    public static DateTimeOffset StepTimeAt(DateTimeOffset time) =>
        new(time.UtcTicks - time.UtcTicks % StepLength.Ticks, TimeSpan.Zero);
}
