namespace ShutterRfBridge;

/// <summary>
///     Transmits commands through a CC1101 in packet mode, one <see cref="RemotePacketEncoder" /> packet per frame.
///     Each command resets and configures the radio first, so no state carries over between commands.
/// </summary>
internal sealed class Cc1101Transmitter : IRemoteTransmitter
{
    private const int FramesPerCommand = 6;

    // SPI header: a read sets bit 7; bit 6 selects burst access, which also tells status registers apart from strobes.
    private const byte ReadStatusRegister = 0xC0;
    private const byte WriteBurst = 0x40;

    private const byte ResetStrobe = 0x30;
    private const byte CalibrateStrobe = 0x33;
    private const byte TransmitStrobe = 0x35;
    private const byte IdleStrobe = 0x36;
    private const byte FlushTxFifoStrobe = 0x3B;

    private const byte VersionRegister = 0x31;
    private const byte MainStateRegister = 0x35;
    private const byte PaTableAddress = 0x3E;
    private const byte TxFifoAddress = 0x3F;

    private const byte ExpectedVersion = 0x14;
    private const byte ChipNotReadyBit = 0x80;
    private const byte MainStateMask = 0x1F;
    private const byte IdleState = 0x01;
    private const byte TxUnderflowState = 0x16;

    // The settings proven on the hardware during the radio's bring-up.
    private static readonly (byte Address, byte Value)[] RegisterSettings =
    [
        (0x02, 0x2E), // IOCFG0: GDO0 unused (high impedance)
        (0x06, RemotePacketEncoder.PacketLength), // PKTLEN
        (0x07, 0x00), // PKTCTRL1
        (0x08, 0x00), // PKTCTRL0: fixed length, no CRC, no whitening
        (0x0D, 0x10), // FREQ2-0: 433.88 MHz with the 26 MHz crystal
        (0x0E, 0xB0),
        (0x0F, 0x0D),
        (0x10, 0x88), // MDMCFG4, MDMCFG3: DRATE_E 8, DRATE_M 83, ~8406 Bd
        (0x11, 0x53),
        (0x12, 0x30), // MDMCFG2: OOK, no preamble or sync word
        (0x13, 0x02), // MDMCFG1
        (0x17, 0x00), // MCSM1: back to IDLE after TX
        (0x18, 0x08), // MCSM0: manual calibration only
        (0x22, 0x11), // FREND0: a 1 bit sends PATABLE[1], a 0 bit PATABLE[0]
        (0x23, 0xE9), // FSCAL3-0
        (0x24, 0x2A),
        (0x25, 0x00),
        (0x26, 0x1F),
        (0x2C, 0x81), // TEST2-0
        (0x2D, 0x35),
        (0x2E, 0x09)
    ];

    // Carrier off for a 0 bit, about 10 dBm for a 1 bit.
    private static readonly byte[] PaTable = [0x00, 0xC0];

    private static readonly TimeSpan StateTimeout = TimeSpan.FromMilliseconds(500);

    private readonly ISpiBus _spi;
    private readonly TimeProvider _timeProvider;

    public Cc1101Transmitter(ISpiBus spi, TimeProvider timeProvider)
    {
        _spi = spi;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The radio isn't a CC1101, or its TX FIFO ran empty.</exception>
    /// <exception cref="TimeoutException">The radio didn't return to IDLE in time.</exception>
    public void Transmit(RemoteFrame frame)
    {
        var packet = RemotePacketEncoder.Encode(frame).ToArray();
        try
        {
            Configure();
            for (var frameIndex = 0; frameIndex < FramesPerCommand; frameIndex++)
            {
                Write(TxFifoAddress, packet);
                Strobe(TransmitStrobe);
                WaitForIdle();
            }
        }
        finally
        {
            Strobe(IdleStrobe);
            Strobe(FlushTxFifoStrobe);
        }
    }

    private void Configure()
    {
        Strobe(ResetStrobe);
        WaitForIdle();
        var version = _spi.Transfer([ReadStatusRegister | VersionRegister, 0])[1];
        if (version != ExpectedVersion)
        {
            throw new InvalidOperationException(
                $"The radio reports version 0x{version:X2}, not a CC1101's 0x{ExpectedVersion:X2}; check its wiring.");
        }

        foreach (var (address, value) in RegisterSettings)
        {
            Write(address, [value]);
        }

        Write(PaTableAddress, PaTable);
        Strobe(CalibrateStrobe);
        WaitForIdle();
    }

    /// <summary>
    ///     Polls the main state until two reads in a row return IDLE. The CC1101 errata says a single read of a status
    ///     register can be wrong while the value changes.
    /// </summary>
    private void WaitForIdle()
    {
        var start = _timeProvider.GetTimestamp();
        byte? previousState = null;
        while (true)
        {
            var state = ReadMainState();
            // A not-ready answer (null) never counts as agreeing, not even with another one.
            if (state is { } readyState && readyState == previousState)
            {
                switch (readyState)
                {
                    case IdleState:
                        return;
                    case TxUnderflowState:
                        throw new InvalidOperationException("The CC1101's TX FIFO ran empty while transmitting.");
                }
            }

            if (_timeProvider.GetElapsedTime(start) > StateTimeout)
            {
                var lastState = state is null ? "chip not ready" : $"main state 0x{state:X2}";
                throw new TimeoutException(
                    $"The CC1101 didn't return to IDLE within {StateTimeout.TotalMilliseconds} ms ({lastState}).");
            }

            previousState = state;
        }
    }

    /// <returns>The main state, or null while the chip isn't ready to answer, such as during a reset.</returns>
    private byte? ReadMainState()
    {
        var read = _spi.Transfer([ReadStatusRegister | MainStateRegister, 0]);
        return (read[0] & ChipNotReadyBit) == 0 ? (byte)(read[1] & MainStateMask) : null;
    }

    private void Write(byte address, byte[] values)
    {
        var header = values.Length > 1 ? (byte)(address | WriteBurst) : address;
        _spi.Transfer([header, .. values]);
    }

    private void Strobe(byte strobe)
    {
        _spi.Transfer([strobe]);
    }
}
