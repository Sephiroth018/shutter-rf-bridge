namespace ShutterRfBridge.Weather;

/// <summary>The settings for the rain at the house, from the <see cref="SectionName" /> configuration section.</summary>
internal class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>The house's location, checked by <see cref="HouseLocation.Read" />.</summary>
    public LocationSettings? Location { get; init; }
}
