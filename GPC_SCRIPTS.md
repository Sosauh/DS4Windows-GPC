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

Example scripts are in `extras\gpc`: `turbo_cross.gpc` (hold X for 10 presses a second) and
`anti_drift.gpc` (radial deadzone with rescale).

For XInput-only games such as Bully, set the profile's emulated controller to **Xbox 360**.

## How it fits

DS4Windows maps the controller as usual; the script then runs on that mapped state, on its own 10 ms
cycle like a Zen (`vm_tctrl` changes it), and every report sent to the virtual controller carries the
latest script output. Profiles without a `.gpc` file are untouched.
