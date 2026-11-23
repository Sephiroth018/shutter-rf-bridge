namespace ShutterRfBridge.Weather;

/// <summary>The rain of one nowcast step: millimetres in the <see cref="Nowcast.StepLength" /> at <see cref="Time" />.</summary>
public record NowcastStep(DateTimeOffset Time, double Millimetres);
