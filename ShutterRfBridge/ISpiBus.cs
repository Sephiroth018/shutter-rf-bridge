namespace ShutterRfBridge;

/// <summary>A full-duplex SPI connection to one device.</summary>
/// <remarks>
///     Array-based so tests can substitute it: NSubstitute can't intercept the span-based members of
///     <see cref="System.Device.Spi.SpiDevice" />.
/// </remarks>
internal interface ISpiBus
{
    /// <summary>Writes <paramref name="write" /> and returns the bytes read meanwhile, as many as were written.</summary>
    byte[] Transfer(byte[] write);
}
