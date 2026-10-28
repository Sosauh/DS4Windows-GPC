# GPC scripts in DS4Windows

This build can run a Zen-style GPC script per profile.

## Use it

1. Open the profile in the editor, go to the **Other** tab, and under **GPC Script** press
   **Choose script...**, then **Save**. (It is stored as `Profiles\<profile>.gpc`, so you can also drop a file
   there by hand.) Scripts with errors are rejected right away with the error lines.
2. Select that profile for your controller, or let **Auto Profiles** select it when `Bully.exe` runs.
   When the game closes, Auto Profiles switches back and the script stops with it.
3. Check the Log tab: it says `GPC: controller 1 running Bully.gpc`, or lists compile errors with line numbers.

Edited a script file by hand? Choose it again, or stop and start DS4Windows, to reload it.

## Screen (OLED)

Scripts can draw on a 128x64 screen like the Zen's: `cls_oled`, `pixel_oled`, `line_oled`, `rect_oled`,
`circle_oled`, `print`, `putc_oled`/`puts_oled` and `image_oled`. Press **Show screen** in the profile's GPC
Script box to open it in a window (it stays on top while you play; untick "Always on top" to change that).
`const string NAMES[] = { "A", "B" };` works for menus: `print(x, y, OLED_FONT_SMALL, OLED_WHITE, NAMES[i])`.
Font sizes other than LARGE (16x26) are a best guess until measured on a Zen.

Example scripts are in `extras\gpc`: `settings_menu.gpc` (turbo + anti-drift with an on-screen menu: hold L2
and press OPTIONS), `turbo_cross.gpc` (hold X for 10 presses a second) and
`anti_drift.gpc` (radial deadzone with rescale).

For XInput-only games such as Bully, set the profile's emulated controller to **Xbox 360**.

## How it fits

DS4Windows maps the controller as usual; the script then runs on that mapped state, on its own 10 ms
cycle like a Zen (`vm_tctrl` changes it), and every report sent to the virtual controller carries the
latest script output. Profiles without a `.gpc` file are untouched.

## Hiding the real controller

"Hide DS4 Controller" (exclusive mode, Settings tab) is on by default in this build: DS4Windows opens the
controller so no other app can read it, and the Log says "Using Exclusive Mode". Start DS4Windows before games and
browsers; if another app already has the controller open, close it and replug. HidHide below is a second layer.

With HidHide installed (1.5.230 or newer), this build hides each connected controller from games automatically, so
games see only the virtual pad. DS4Windows keeps reading it because it adds itself to HidHide's allowed apps at
startup. HidHide applies the change when the controller reconnects: unplug and replug it once after the first run.
A game or browser that already had it open keeps seeing it until it is restarted. The hiding stays in place when
DS4Windows closes; to use the controller directly, untick "Enable device hiding" in HidHide. If the Log tab says
HidHide could not be changed, run DS4Windows as administrator once.
