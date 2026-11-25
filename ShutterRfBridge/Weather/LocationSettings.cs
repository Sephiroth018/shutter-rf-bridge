namespace ShutterRfBridge.Weather;

/// <summary>
///     The house's coordinates as written in the settings. Text, so that the configuration binder can't fail the startup
///     over a broken value; <see cref="Location.Read" /> checks them.
/// </summary>
internal class LocationSettings
{
    public string? Latitude { get; init; }
    public string? Longitude { get; init; }
}
