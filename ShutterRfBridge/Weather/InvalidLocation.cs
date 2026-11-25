namespace ShutterRfBridge.Weather;

/// <summary>A <see cref="Location" /> missing from the settings or broken there, with the problem naming the key.</summary>
public sealed record InvalidLocation(string Problem) : Location;
