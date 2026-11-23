namespace ShutterRfBridge.Weather;

/// <summary>The settings for the rain at the house, from the <see cref="SectionName" /> configuration section.</summary>
public class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>GeoSphere's Dataset API, with a trailing slash so the dataset paths append to it.</summary>
    public Uri? ApiAddress { get; init; }

    /// <summary>The house's location; with neither coordinate set, the rain is always unknown.</summary>
    public Coordinates? Location { get; init; }

    /// <summary>How old the nowcast's run may be before its rain counts as unknown.</summary>
    public TimeSpan StalenessLimit { get; init; }

    /// <summary>How long the request for the nowcast may take.</summary>
    public TimeSpan RequestTimeout { get; init; }
}
