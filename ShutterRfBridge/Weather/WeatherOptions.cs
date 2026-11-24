namespace ShutterRfBridge.Weather;

/// <summary>The settings for the rain at the house, from the <see cref="SectionName" /> configuration section.</summary>
public class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>The house's location; without it, the rain is always unknown.</summary>
    public Coordinates? Location { get; init; }
}
