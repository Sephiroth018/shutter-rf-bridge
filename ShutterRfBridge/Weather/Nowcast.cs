namespace ShutterRfBridge.Weather;

/// <summary>
///     One run of the rain nowcast for one point and one step: the run's reference time and the step's millimetres per
///     <see cref="StepLength" />, or <see langword="null" /> when the run has no value for the step.
/// </summary>
public record Nowcast(DateTimeOffset ReferenceTime, double? Millimetres)
{
    /// <summary>The nowcast's time resolution.</summary>
    public static readonly TimeSpan StepLength = TimeSpan.FromMinutes(15);

    /// <summary>The time of the step <paramref name="time" /> falls in: rounded down to <see cref="StepLength" />, in UTC.</summary>
    public static DateTimeOffset StepTimeAt(DateTimeOffset time) =>
        new(time.UtcTicks - time.UtcTicks % StepLength.Ticks, TimeSpan.Zero);
}
