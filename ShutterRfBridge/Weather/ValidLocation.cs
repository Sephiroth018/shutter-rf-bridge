namespace ShutterRfBridge.Weather;

/// <summary>A <see cref="Location" /> the nowcast can be fetched for.</summary>
public sealed record ValidLocation(Coordinates Coordinates) : Location;
