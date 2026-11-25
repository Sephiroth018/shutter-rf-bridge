namespace ShutterRfBridge.Weather;

/// <summary>A <see cref="HouseLocation" /> the nowcast can be fetched for.</summary>
public sealed record UsableHouseLocation(Coordinates Coordinates) : HouseLocation;
