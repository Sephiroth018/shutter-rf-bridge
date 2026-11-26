# shutter-rf-bridge

Controls radio-controlled external blinds and roller shutters from a Homematic CCU3. A service on
a Raspberry Pi 4 sends the shutters' 433 MHz remote control codes through a CC1101 transceiver
and offers them as an HTTP API, which CUxD devices on the CCU3 call. As a bonus, it tells the CCU3
how much rain falls at the configured coordinates, from GeoSphere Austria's rain nowcast.

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

The local settings live in `/etc/shutter-rf-bridge/appsettings.json`, which overrides the service's
built-in settings:

```bash
sudo nano /etc/shutter-rf-bridge/appsettings.json
```

Fill in the values as described in [Configuration](#configuration). Once the service is
installed, restart it after every change:

```bash
sudo systemctl restart shutter-rf-bridge
```

### Update

To update, run the deploy script again, while no shutter is moving: a restart drops pending timed
Stops, so a shutter in a partial move would run to its end. Without new commits, it leaves the
running service alone. After system updates, restart the
service so it runs on the patched .NET.

The deploy script never changes the settings file, so settings added in a newer version have to be
added by hand. For the rain, if the settings file has no `"Weather"` line yet, open it:

```bash
sudo nano /etc/shutter-rf-bridge/appsettings.json
```

Paste this block on its own line right after the `},` that closes the `"Bridge"` block, fill in
the coordinates as described in [Configuration](#configuration), save with Ctrl+O and Enter, and
exit with Ctrl+X:

```json
  "Weather": {
    "Location": {
      "Latitude": "",
      "Longitude": ""
    }
  },
```

Then restart the service:

```bash
sudo systemctl restart shutter-rf-bridge
```

### Logs

The service's log (commands received and sent, errors) goes to two places (see
[Logging](#logging)):

- **systemd's journal**, next to the service's starts, stops and crashes, including a missing or
  wrong setting at startup. Raspberry Pi OS keeps the journal in memory, so it starts empty after a
  reboot. To watch it live:

  ```bash
  journalctl -u shutter-rf-bridge -f
  ```

  To see only the errors:

  ```bash
  journalctl -u shutter-rf-bridge -p err
  ```

- **`/var/log/shutter-rf-bridge`**, the history that survives a reboot, written in batches.

### Uninstall

Uninstall while no shutter is moving, since stopping the service drops pending timed Stops (see
[Update](#update)). The uninstall script stops the service and removes it, its program, its logs and
its system user, and is safe to run again:

```bash
~/shutter-rf-bridge/deploy/uninstall.sh
```

The settings are kept, so running the deploy script again reinstalls the service as it was. To
remove them as well, note the local settings first, if you still need them:

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

The CCU3 calls the service through CUxD: a device whose channels send the shutters' commands, and
a timer and a program that bring in the rain. [Set up the CCU3](docs/ccu3-setup.md) describes
both. The CCU3's IP address must be in the service's allow-list (see [Settings](#settings)); the
service rejects calls from any other address.

### Known limitations

- **The radio is one-way.** After the physical remote has been used, the CCU's position estimate is
  wrong until the next full Up or Down.
- **The CCU can't stop a shutter it doesn't think is moving**, such as one started with the
  physical remote. Use the remote instead.
- **Run times are at most 65 seconds** (see [Channels](docs/ccu3-setup.md#channels)).
- **A restart of the service drops pending timed Stops** (see [Update](#update)).
- **The rain is radar-based:** it misses light drizzle and very local showers, and lags by about
  30 minutes. Reporting the next 45 minutes makes up for the lag, but means shutters also close
  for a shower that passes by. That's enough for "keep closed while it rains", not for protecting
  anything from a sudden shower.

## HTTP API

Every request from a client that isn't in `Bridge:AllowedClients` (see
[Configuration](#configuration)) is answered with `403 Forbidden`, whatever the path.

### Shutters

Every shutter endpoint is a `POST` without a body, with the shutter's channel on the remote (0–15)
in the path:

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

### Rain

`GET /weather/rain` answers `200 OK` with one line of plain text, unless the location is invalid:

- **A number:** the millimetres of rain at the configured coordinates in the heaviest of the
  current 15 minutes and the next two, leaving out any without a value, with a decimal point
  (`0`, `0.3`). Looking ahead makes up for the nowcast's lag of about 30 minutes, so the rain
  shows before it arrives. Snow counts as rain: the nowcast gives all precipitation as
  millimetres of water, so 1 mm is about 1 cm of fresh snow.
- **`unknown`:** GeoSphere didn't answer within 15 seconds or answered with an error or something
  unreadable, its latest nowcast run is older than an hour, or the run has no value for any of
  the three steps.

Without a valid location (the `Location` block left out, or a coordinate missing, empty, not a
number or out of range), it answers `500 Internal Server Error` without a body, and the log names
the problem; the CCU3's `curl -f` then writes `unknown`.

Each request fetches these three steps of GeoSphere Austria's 1 km rain nowcast
(`nowcast-v1-15min-1km`); nothing is cached. To try it on the Pi:

```bash
curl http://127.0.0.1:5080/weather/rain
```

The rain data comes from [GeoSphere Austria](https://data.hub.geosphere.at/), under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

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
| `Bridge:AllowedClients` | The IP addresses allowed to send commands, as a list: the CCU's, and `127.0.0.1` for checks with `curl` on the Pi. |
| `Weather:Location:Latitude`, `Weather:Location:Longitude` | The coordinates for the nowcast endpoint, in decimal degrees. In the settings file, remove the `//` in front of the `Location` block to fill them in. Without both, the shutters still work, but the rain endpoint answers an error. |
| `Urls` | The address and port the service listens on: `http://0.0.0.0:5080` in the settings file. |

The service doesn't start if the remote ID is missing or too large, the allow-list is empty, or an
entry isn't an IP address; the journal says which. An invalid location doesn't stop it: it logs
an error at startup naming the problem.

**Development:** run without hardware, with `dotnet run --project ShutterRfBridge`. Its launch
profile uses the Development environment, listening on `http://localhost:5080`. Development only
logs what it would send over the radio, uses a made-up remote ID and allows `127.0.0.1` and `::1`.
Its made-up location is in Austria, so the rain endpoint fetches real nowcast data.

## Logging

The service logs with [NLog](https://nlog-project.org/):

- **Debug:** each rain amount answered.
- **Information:** each command received, each timed Stop queued and each command sent.
- **Warning:** refused and rejected requests, and each rain request without rain data, with the
  reason. During a GeoSphere outage that's one entry every 15 minutes.
- **Error:** an invalid location, once at startup; a command that failed to send, after which the
  service goes on with the next one; and an unexpected error, which stops the service so that
  systemd restarts it (up to 5 times in 5 minutes).

ASP.NET Core's own entries below warning level are left out, and the request to GeoSphere isn't
logged by .NET itself, to spare the SD card.

**Defaults:**

- **File:** `/var/log/shutter-rf-bridge/shutter-rf-bridge.log`, archived daily, with archives kept for
  31 days and the folder capped at about 10 MB.
- **Buffering:** entries are written to the file after 100 entries or 5 minutes, whichever comes
  first; errors are written at once.
- **Console:** every entry is also written to the console at once, as one line that starts with its
  level. Under systemd, the lines go to the journal, each with its level as the journal's priority.
  Raspberry Pi OS keeps the journal in memory; on a system that stores it on the SD card, these
  lines are written there unbuffered.
- **Development** logs to the console only, with the time and without the priority.

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
