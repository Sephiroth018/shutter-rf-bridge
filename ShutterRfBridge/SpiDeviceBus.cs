using System.Device.Spi;

namespace ShutterRfBridge;

/// <summary>The <see cref="ISpiBus" /> on the Raspberry Pi's SPI0 CE0, where the CC1101 is wired.</summary>
internal sealed class SpiDeviceBus : ISpiBus, IDisposable
{
    private const int BusId = 0;
    private const int ChipSelectLine = 0;
    private const int ClockFrequencyHz = 500_000;

    private readonly SpiDevice _device = SpiDevice.Create(
        new SpiConnectionSettings(BusId, ChipSelectLine) { ClockFrequency = ClockFrequencyHz, Mode = SpiMode.Mode0 });

    public void Dispose()
    {
        _device.Dispose();
    }

    public byte[] Transfer(byte[] write)
    {
        var read = new byte[write.Length];
        _device.TransferFullDuplex(write, read);
        return read;
    }
}
