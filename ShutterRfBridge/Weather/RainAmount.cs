using System.Globalization;

namespace ShutterRfBridge.Weather;

/// <summary>The rain in the current step, in millimetres per <see cref="Nowcast.StepLength" />.</summary>
internal sealed record RainAmount(double Millimetres) : RainReport
{
    public override RainCondition Condition => Millimetres > 0 ? RainCondition.Raining : RainCondition.Dry;

    // HM script's ToFloat reads a decimal point, whatever the Pi's culture.
    public override string ToAnswer() => Millimetres.ToString(CultureInfo.InvariantCulture);
}
