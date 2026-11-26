namespace ShutterRfBridge.Weather;

/// <summary>A <see cref="Location" /> missing from the settings or broken there, with the problem naming the key.</summary>
internal sealed record InvalidLocation(string Problem) : Location;
