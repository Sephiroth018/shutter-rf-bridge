namespace ShutterRfBridge.Weather;

/// <summary>
///     A point on the earth in decimal degrees. Both are nullable because the settings template leaves them empty,
///     which binds as <see langword="null" />.
/// </summary>
public class Coordinates
{
    public const double MaxLatitude = 90;
    public const double MaxLongitude = 180;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
