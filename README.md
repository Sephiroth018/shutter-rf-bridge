# shutter-rf-bridge

Controls radio-controlled external blinds and roller shutters from a Homematic CCU3. A service on
a Raspberry Pi 4 sends the shutters' 433 MHz remote control codes through a CC1101 transceiver
and offers them as an HTTP API, which CUxD devices on the CCU3 call.

The project is being set up; there's nothing to install or use yet.

## Build

Requires the .NET SDK version in `global.json`.

```powershell
dotnet tool restore
dotnet build ShutterRfBridge.slnx
```

## AI disclaimer

This project is AI-assisted: Claude Code does most of the implementation, under the direction
and review of the maintainer. Commits written by Claude carry a Co-Authored-By: Claude trailer,
and everything it posts on GitHub is marked as generated with Claude Code.

## License

[MIT](LICENSE)
