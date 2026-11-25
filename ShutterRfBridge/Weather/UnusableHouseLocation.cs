namespace ShutterRfBridge.Weather;

/// <summary>A <see cref="HouseLocation" /> missing from the settings or broken there, with the problem naming the key.</summary>
public sealed record UnusableHouseLocation(string Problem) : HouseLocation;
