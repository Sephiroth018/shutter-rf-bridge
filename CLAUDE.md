# CLAUDE.md — shutter-rf-bridge

Three parts: **General** applies to any project, **C#** to any C# project, **Project** to this one
only; where it overrides a General or C# rule, it wins. General and C# are the shared files
imported below. Instructions never go in HTML comments; they're stripped before this file is
loaded.

@.claude/shared/general.md

@.claude/shared/csharp.md

# Project

A .NET service on a Raspberry Pi 4 that sends 433 MHz remote control codes to radio-controlled
shutters through a CC1101 transceiver, controlled over HTTP by a Homematic CCU3 (see README.md).
The repository is a public GitHub repository, `Sephiroth018/shutter-rf-bridge`.

## Environment

- **The Pi runs Raspberry Pi OS 64-bit (Debian Trixie)**, on Wi-Fi only, and is reached through
  RDP or SSH. The service runs as a systemd service, independent of any desktop session.
- **Logs persist on the SD card**, so the service logs only real events at info level, never in
  loops.
- **Local settings stay out of the repository:** the remote's ID, the Pi's address and the
  channel mapping live in local configuration on the Pi.
- **The user isn't fond of terminal work:** instructions for the Pi are complete copy-paste
  command blocks. Hardware suggestions are plug-only: no soldering, cutting or modifying cases.

## Verification

The Definition of done's steps, which CI runs. Changes that reach the radio or the CCU3 are also
tested by the user on the hardware, as the checklist in the PR's description.

## Versioning

None yet: the service is deployed from `main`.
