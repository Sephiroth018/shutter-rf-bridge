# Set up OpenCCU

How an OpenCCU instance (the CCU below) controls the shutters and reads the rain through
[shutter-rf-bridge](../README.md), using CUxD. Labels are given as the German WebUI shows them.

## Contents

- [Prerequisites](#prerequisites)
- [Shutters](#shutters)
  - [Device](#device)
  - [Channels](#channels)
- [Rain](#rain)
  - [System variables](#system-variables)
  - [Timer](#timer)
  - [Program](#program)
  - [Other programs](#other-programs)

## Prerequisites

- **CUxD** is installed on the CCU.
- **The CCU's IP address is in the service's allow-list**, `Bridge:AllowedClients` (see
  [Configuration](../README.md#configuration)); the service rejects calls from any other address.

In the commands below, replace `<pi>` with the Pi's IP address. The port is the one in `Urls` in
the service's settings file, 5080 by default.

## Shutters

### Device

Create one CUxD **(40) 16-Kanal Universalsteuerung** device. Each shutter gets one of its channels,
with the control set to **Rolladen**.

### Channels

In each shutter's channel settings:

1. Tick `BLIND|CMD_EXEC`.
2. Enter the three commands. Replace `<channel>` with the shutter's channel on the physical remote
   (0–15), which needn't match the CUxD channel number:

   | Field | Command |
   |---|---|
   | `BLIND\|CMD_LONG` (Up) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/up/$VALUE$/$RUNTIME$` |
   | `BLIND\|CMD_SHORT` (Down) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/down/$VALUE$/$RUNTIME$` |
   | `BLIND\|CMD_STOP` (Stop) | `curl -s -m 2 -X POST http://<pi>:5080/shutters/<channel>/stop` |

3. Set **Fahrzeit von oben nach unten** and **Fahrzeit von unten nach oben** (in seconds) slightly
   longer than the shutter really takes, so CUxD treats it as moving, and Stop works, until it has
   arrived. Use at most 65 seconds, although the fields take up to 255: CUxD sends `$RUNTIME$` as a
   signed 16-bit number of milliseconds, so a longer run time arrives as a much shorter one, and
   partial moves then stop far too early.

## Rain

The rain comes in through a string system variable that a CUxD timer fills every 15 minutes, with
no HM script doing the fetch: a script that waits for a command's output stops every other script on
the CCU meanwhile. A program then turns the text into a number and a state.

### System variables

Create three system variables. Their names are free, as long as the timer's command and the
program's script use the same ones:

| Name | Type | Values |
|---|---|---|
| `Rain_Raw` | Zeichenkette | The service's answer, e.g. `0.3` or `unknown` |
| `Rain_mm` | Zahl, unit `mm` | The rain in the heaviest 15 minutes of the next 45 |
| `Rain` | Werteliste | `dry;rain;unknown` |

### Timer

Create one CUxD **(28) System** device with the function **Timer**. In its first channel's
settings:

| Field | Value |
|---|---|
| `TIMER_PRESET` | `:0:0/:15:0/:30:0/:45:0` |
| `REPEAT` | ticked |
| `CMD_EXEC` | `extra/timer.tcl Rain_Raw ~22$(curl -sf -m 20 http://<pi>:5080/weather/rain \|\| echo unknown)~22` |
| `EXEC_TIMEOUT` | `1` |

If the service doesn't answer or answers an error, `curl` fails and `unknown` is written instead.
If the timer doesn't start by itself, start it once from a script, with the timer device's serial
number in place of `CUX2800001`:

```
dom.GetObject("CUxD.CUX2800001:1.TIMER_SET").State(":0:0/:15:0/:30:0/:45:0");
```

### Program

Create a program with the condition **Systemzustand `Rain_Raw`, bei Änderung auslösen**, with the
text field left empty. It only triggers the program, since the WebUI's text comparison doesn't
pick the branch reliably. So **Dann** and **Sonst** both get the same script, which sets the amount,
and the state by a threshold, here 0.1 mm:

```
var raw = dom.GetObject("Rain_Raw").Value();
if (raw == "unknown") {
  dom.GetObject("Rain").State(2);
} else {
  var mm = raw.ToFloat();
  dom.GetObject("Rain_mm").State(mm);
  if (mm >= 0.1) { dom.GetObject("Rain").State(1); } else { dom.GetObject("Rain").State(0); }
}
```

To test it, run `dom.GetObject("Rain_Raw").State("unknown");` in **Skript testen**, then the same
with `"0.5"` and `"0"`: `Rain` switches to unknown, rain and dry.

### Other programs

Other programs trigger on `Rain` with bei Änderung auslösen, so they run only when the state
changes, or compare `Rain_mm` with their own threshold, checking first that `Rain` isn't unknown.
