namespace ShutterRfBridge.Weather;

/// <summary>A point on the earth in decimal degrees.</summary>
public record Coordinates(double Latitude, double Longitude)
{
    public const double MaxLatitude = 90;
    public const double MaxLongitude = 180;
}
