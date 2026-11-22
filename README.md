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

The service runs as the systemd service `shutter-rf-bridge`. Run each block as a whole.

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

The house values live in `/etc/shutter-rf-bridge/appsettings.json`, which overrides the service's
built-in settings:

```bash
sudo nano /etc/shutter-rf-bridge/appsettings.json
```

Fill in `RemoteId` and the CCU's IP address as the first entry of `AllowedClients`; the comments in
the file explain each value, and [Configuration](#configuration) has the details. Once the service
is installed, restart it after every change:

```bash
sudo systemctl restart shutter-rf-bridge
```

### Update

To update, run the deploy script again. Without new commits, it leaves the running service alone.
After system updates, restart the service so it runs on the patched .NET.

A restart, like any stop of the service, first sends the pending timed Stops, so a shutter in a
partial move still stops at its target. While one is pending, the restart takes up to its run
time, at most about 75 seconds; the service still takes commands meanwhile.

### Logs

systemd's journal has the service's starts, stops and crashes, including a missing or wrong setting
at startup. The service's own log (commands received and sent, errors) is in
`/var/log/shutter-rf-bridge`, written in batches (see [Logging](#logging)). To watch it live, remove
the `//` in front of the `"NLog"` line in the settings file and restart the service. Put the `//`
back afterwards, to spare the SD card.

### Uninstall

The uninstall script stops the service, after its pending timed Stops (see [Update](#update)), and
removes it, its program, its logs and its system user. It is safe to run again:

```bash
~/shutter-rf-bridge/deploy/uninstall.sh
```

The settings are kept, so running the deploy script again reinstalls the service as it was. To
remove them as well, note the house values first, if you still need them:

```bash
sudo cat /etc/shutter-rf-bridge/appsettings.json
```

Then run the script with `--purge`:

```bash
~/shutter-rf-bridge/deploy/uninstall.sh --purge
```

The clone with the scripts stays. Remove it last; a later install then starts again with
[First install](#first-install):

```bash
rm -rf ~/shutter-rf-bridge
```

The service's entries in systemd's journal remain until the journal rotates them out. The .NET SDK,
Microsoft's package repository and SPI are left as they are, since other software may use them.

## Set up the CCU3

The CCU3 calls the service through CUxD. The CCU3's IP address must be in the service's allow-list
(see [Settings](#settings)); the service rejects calls from any other address.

### Device

Create one CUxD **(40) 16-Kanal Universalsteuerung** device and accept it from the WebUI's inbox.
Each shutter gets one of its channels, with the control set to **Rolladen**.

### Channels

In each shutter's channel settings:

1. Tick `BLIND|CMD_EXEC`.
2. Enter the three command lines. Replace `<pi>` with the Pi's IP address, and `<channel>` with the
   shutter's channel on the physical remote (0–15), which needn't match the CUxD channel number.
   The port is the one in `Urls` in the settings file, 5080 by default:

   | Field | Command |
   |---|---|
   | `BLIND\|CMD_LONG` (Up) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/up/$VALUE$/$RUNTIME$` |
   | `BLIND\|CMD_SHORT` (Down) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/down/$VALUE$/$RUNTIME$` |
   | `BLIND\|CMD_STOP` (Stop) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/stop` |

   `-X POST` is required: without it, curl sends a GET, which the service rejects.
3. Set **Fahrzeit von oben nach unten** and **Fahrzeit von unten nach oben** (in seconds) slightly
   longer than the shutter really takes, so CUxD treats it as moving, and Stop works, until it has
   arrived. Use at most 65 seconds, although the fields take up to 255: CUxD sends `$RUNTIME$` as a
   signed 16-bit number of milliseconds, so a longer run time arrives as a much shorter one, and
   partial moves then stop far too early.

The channel's other fields aren't needed.

### Known limitations

- **The radio is one-way.** After the physical remote has been used, the CCU's position estimate is
  wrong until the next full Up or Down.
- **The CCU can't stop a shutter it doesn't think is moving**, such as one started with the
  physical remote. Use the remote instead.
- **Run times are at most 65 seconds** (see step 3 above).
- **A restart waits for pending timed Stops**, up to about 75 seconds (see [Update](#update)). A
  crash or power cut still drops them, so a shutter in a partial move runs to its end.

## HTTP API

Every endpoint is a `POST` without a body, with the shutter's channel on the remote (0–15) in the
path:

| Endpoint | Command |
|---|---|
| `/shutters/{channel}/up/{level}/{runtimeMs}` | Up, to `level` |
| `/shutters/{channel}/down/{level}/{runtimeMs}` | Down, to `level` |
| `/shutters/{channel}/stop` | Stop |

`level` is the target position from 0 (closed) to 1000 (open), as CUxD's `$VALUE$`, and
`runtimeMs` is how long the move takes in milliseconds, as CUxD's `$RUNTIME$`.

The service answers:

- **`202 Accepted`** at once, with the command queued.
- **`400 Bad Request`** for a channel or level out of range, a value that isn't a number, or a run
  time of 0 or outside −32 768…32 767.
- **`403 Forbidden`** for a client that isn't in `Bridge:AllowedClients` (see
  [Configuration](#configuration)), whatever the path.

**Moves:**

- **A move to a level between 1 and 999 is partial:** the service sends a Stop once its run time
  has passed. The run time starts when the command is sent, not when the request arrives, so a
  backlog doesn't shorten it.
- **A move to 0 or 1000 is full:** no Stop follows, since the motor stops at its end position by
  itself.
- **Run times are 16-bit numbers:** CUxD sends `$RUNTIME$` as a signed 16-bit number, so run times
  above 32 767 ms arrive negative. The service reads negative values as unsigned, which allows run
  times up to 65 535 ms. Sent by hand, such a run time is negative too: 35 000 ms is `-30536`.

**Queue:** the radio sends one command at a time, about 0.35 s each. A Stop, and a move for a
shutter that is still moving, go ahead of the other queued moves. Any new command for a shutter cancels its
pending timed Stop, and a Stop also removes the shutter's queued commands.

To try it on the Pi itself, which the settings file allows by default, this moves the shutter on
channel 3 halfway down over 10 seconds:

```bash
curl -i -X POST http://127.0.0.1:5080/shutters/3/down/500/10000
```

## Configuration

The settings come from these sources, each overriding the ones before it:

1. **`appsettings.json`** next to the program: the built-in defaults, such as logging. A deploy
   replaces it, so it isn't the place for changes.
2. **`appsettings.Development.json`**, in the Development environment only.
3. **`/etc/shutter-rf-bridge/appsettings.json`**, the settings file on the Pi (see
   [Settings](#settings)), read only when the service runs under systemd.
4. **Environment variables**, with `__` for each `:` in a setting's name
   (`Bridge__RemoteId`).

The settings:

| Setting | Value |
|---|---|
| `Bridge:RemoteId` | The remote's ID, as a decimal number or `0x…` hex, in quotes; at most 28 bits. |
| `Bridge:AllowedClients` | The IP addresses allowed to send commands, as a list. |
| `Urls` | The address and port the service listens on: `http://0.0.0.0:5080` in the settings file. |

The service doesn't start if the remote ID is missing or too large, the allow-list is empty, or an
entry isn't an IP address; the journal says which.

**Development:** run without hardware, with `dotnet run --project ShutterRfBridge`. Its launch
profile uses the Development environment, listening on `http://localhost:5080`. Development only
logs what it would send over the radio, uses a made-up remote ID and allows `127.0.0.1` and `::1`.

## Logging

The service logs with [NLog](https://nlog-project.org/):

- **Information:** each command received, each timed Stop queued and each command sent, and a stop
  that waits for pending commands.
- **Warning:** refused and rejected requests, and a stop that ends with commands still pending.
- **Error:** a command that failed to send, after which the service goes on with the next one, and
  an unexpected error, which stops the service so that systemd restarts it (up to 5 times in 5
  minutes).

ASP.NET Core's own entries below warning level are left out, to spare the SD card.

**Defaults:**

- **File:** `/var/log/shutter-rf-bridge/shutter-rf-bridge.log`, archived daily, with archives kept for
  31 days and the folder capped at about 10 MB.
- **Buffering:** entries are written after 100 entries or 5 minutes, whichever comes first; errors
  are written at once.
- **Development** logs to the console only.

NLog is configured in the `NLog` section of the settings. To change it, override that section in
the settings file on the Pi, as described in NLog's
[configuration with appsettings.json](https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json).

**Under systemd**, the service can only write to `/var/log/shutter-rf-bridge`: the unit protects
the rest of the file system. Logging to another folder needs a `ReadWritePaths=` entry for it in a
drop-in (`sudo systemctl edit shutter-rf-bridge`).

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
