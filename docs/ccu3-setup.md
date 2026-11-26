# Set up the CCU3

The CCU3 calls the service through CUxD. The CCU3's IP address must be in the service's allow-list
(see [Settings](../README.md#settings)); the service rejects calls from any other address.

## Device

Create one CUxD **(40) 16-Kanal Universalsteuerung** device and accept it from the WebUI's inbox.
Each shutter gets one of its channels, with the control set to **Rolladen**.

## Channels

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

## Rain

The rain comes in through a string system variable that a CUxD timer fills every 15 minutes, with
no HM script doing the fetch: a script that waits for a command's output stops every other script on
the CCU3 meanwhile. A program then turns the text into a number and a dry/rain state.

1. Create three system variables:

   | Name | Type | Values |
   |---|---|---|
   | `Regen_Pi` | Zeichenkette | The service's answer, e.g. `0.3` or `unknown` |
   | `Regen_mm` | Zahl, unit `mm` | The rain in the heaviest 15 minutes of the next 45 |
   | `Regen` | Werteliste | `trocken;Regen;unbekannt` |

2. Create one CUxD **(28) System** device with the function **Timer** and accept it from the
   WebUI's inbox. In its first channel's settings:

   | Field | Value |
   |---|---|
   | `TIMER_PRESET` | `:0:0/:15:0/:30:0/:45:0` |
   | `REPEAT` | ticked |
   | `CMD_EXEC` | `extra/timer.tcl Regen_Pi ~22$(curl -sf -m 20 http://<pi>:5080/weather/rain \|\| echo unknown)~22` |
   | `EXEC_TIMEOUT` | `1` |

   `~22` is CUxD's spelling of `"` in these fields. `timer.tcl` writes the answer into `Regen_Pi`;
   if the Pi doesn't answer, `curl` fails and `unknown` is written instead. If the timer doesn't
   start by itself, start it once from a script:
   `dom.GetObject("CUxD.CUX2800001:1.TIMER_SET").State(":0:0/:15:0/:30:0/:45:0");`

3. Create a program with the condition **Systemzustand `Regen_Pi`, bei Änderung auslösen**, with
   the text field left empty. It only triggers the program, since the WebUI's text comparison
   doesn't pick the branch reliably. So **Dann** and **Sonst** both get the same script, which
   sets the amount, and the state by a threshold of your choice:

   ```
   var raw = dom.GetObject("Regen_Pi").Value();
   if (raw == "unknown") {
     dom.GetObject("Regen").State(2);
   } else {
     var mm = raw.ToFloat();
     dom.GetObject("Regen_mm").State(mm);
     if (mm >= 0.1) { dom.GetObject("Regen").State(1); } else { dom.GetObject("Regen").State(0); }
   }
   ```

   To test it, run `dom.GetObject("Regen_Pi").State("unknown");` in Skript testen, then the same
   with `"0.5"` and `"0"`: `Regen` switches to unbekannt, Regen and trocken.

Other programs react to `Regen` with bei Änderung auslösen, so they run only when the state
changes, or compare `Regen_mm` with their own threshold, checking first that `Regen` isn't unknown.
