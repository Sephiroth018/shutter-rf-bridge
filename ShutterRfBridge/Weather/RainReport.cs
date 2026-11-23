namespace ShutterRfBridge.Weather;

/// <summary>The rain at the house in the current nowcast step, as the CCU3 is told it.</summary>
internal abstract record RainReport
{
    /// <summary>Whether it's dry, raining or unknown, which is what the log reports changes of.</summary>
    public abstract RainCondition Condition { get; }

    /// <summary>
    ///     The rain in <paramref name="nowcast" />'s step at <paramref name="now" />, or unknown when the run is older
    ///     than <paramref name="stalenessLimit" /> or has no such step.
    /// </summary>
    public static RainReport From(Nowcast nowcast, DateTimeOffset now, TimeSpan stalenessLimit)
    {
        if (now - nowcast.ReferenceTime > stalenessLimit)
        {
            return new RainUnknown(
                $"the latest nowcast run, from {nowcast.ReferenceTime:u}, is older than {stalenessLimit}");
        }

        var stepTime = Nowcast.StepTimeAt(now);
        return nowcast.Step is { } currentStep && currentStep.Time == stepTime
            ? new RainAmount(currentStep.Millimetres)
            : new RainUnknown($"the nowcast has no value for {stepTime:u}");
    }

    /// <summary>The plain-text answer for the CCU3: one line, without <c>&lt;</c> or <c>&gt;</c>, which CUxD drops.</summary>
    public abstract string ToAnswer();
}
