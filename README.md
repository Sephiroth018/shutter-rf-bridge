# shutter-rf-bridge

Controls radio-controlled external blinds and roller shutters from a Homematic CCU3. A service on
a Raspberry Pi 4 sends the shutters' 433 MHz remote control codes through a CC1101 transceiver
and offers them as an HTTP API, which CUxD devices on the CCU3 call.

## Requirements

- **A Raspberry Pi 4 with Raspberry Pi OS 64-bit (Debian Trixie)**, with the CC1101 on SPI0 CE0.
- **SPI turned on.** If it isn't yet, this turns it on and restarts the Pi:

  ```bash
  sudo raspi-config nonint do_spi 0 && sudo reboot
  ```

- **The .NET 10 SDK**, at least the version in `global.json`, **and git**. On the Pi, they come
  from Microsoft's package repository, so the system's updates keep .NET patched:

  ```bash
  wget -q https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb && sudo dpkg -i /tmp/packages-microsoft-prod.deb && rm /tmp/packages-microsoft-prod.deb && sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0 git
  ```

## Install on the Raspberry Pi

Run each block as a whole, over SSH or in a terminal over RDP.

### First install

Get the service and run its deploy script:

```bash
git clone https://github.com/Sephiroth018/shutter-rf-bridge.git ~/shutter-rf-bridge && ~/shutter-rf-bridge/deploy/deploy.sh
```

The first run creates the settings file and stops. Fill it in (see [Settings](#settings)), then run
the script again, which builds and starts the service:

```bash
~/shutter-rf-bridge/deploy/deploy.sh
```

### Settings

The house values live in `/etc/default/shutter-rf-bridge`:

```bash
sudo nano /etc/default/shutter-rf-bridge
```

Fill in `Bridge__RemoteId` and the CCU's IP address in `Bridge__AllowedClients__0`; the comments in
the file explain each value. Once the service is installed, restart it after every change:

```bash
sudo systemctl restart shutter-rf-bridge
```

### Update

To update, run the deploy script again, while no shutter is moving: a restart drops pending timed
Stops, so a shutter in a partial move would run to its end.

.NET's security patches come with the system's updates. Restart the service afterwards, so it runs
on the patched version:

```bash
sudo apt-get update && sudo apt-get full-upgrade -y && sudo systemctl restart shutter-rf-bridge
```

### Logs

Whether the service is running:

```bash
systemctl status shutter-rf-bridge
```

Its starts, stops and crashes, including a missing or wrong setting at startup:

```bash
journalctl -u shutter-rf-bridge -n 50 --no-pager
```

The service's own log (commands received and sent, errors) is in `/var/log/shutter-rf-bridge`. Its
lines are written in batches, after 100 lines or 5 minutes; errors at once. To watch it live,
remove the `#` in front of `NLog__targets__file__target__bufferSize=1` in the settings file and
restart the service. Put the `#` back afterwards, to spare the SD card.

## Build

```powershell
dotnet tool restore
dotnet build ShutterRfBridge.slnx
dotnet test --solution ShutterRfBridge.slnx
```

Before pushing, format and inspect the code as CI does (`jb cleanupcode` must leave no changes, and
`jb inspectcode` must report nothing). Both skip your personal ReSharper settings, as CI's runs do:

```powershell
dotnet jb cleanupcode ShutterRfBridge.slnx --disable-settings-layers="GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal"
dotnet jb inspectcode ShutterRfBridge.slnx --disable-settings-layers="GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal" --severity=WARNING --output=inspect.sarif
```

## AI disclaimer

This project is AI-assisted: [Claude Code](https://claude.com/claude-code) does most of the
implementation, under the direction and review of the maintainer. Commits written by Claude carry
a `Co-Authored-By: Claude` trailer, and everything it posts on GitHub is marked as generated with
Claude Code.

## License

[MIT](LICENSE)
