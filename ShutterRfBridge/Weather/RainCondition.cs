namespace ShutterRfBridge.Weather;

/// <summary>The rain as the log reports its changes; the CCU3 applies its own threshold to the amount.</summary>
public enum RainCondition
{
    Dry,
    Raining,
    Unknown
}
