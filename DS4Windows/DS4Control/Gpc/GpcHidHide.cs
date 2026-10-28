using System;
using System.Collections.Generic;
using System.Linq;
using DS4WinWPF.DS4Control;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Hides connected controllers from games through HidHide, so games see only the virtual pad.
    /// DS4Windows stays able to read them because it whitelists itself in HidHide at startup.
    /// Entries stay in place when DS4Windows stops: HidHide applies changes only when a controller
    /// reconnects, so undoing them on every stop left the controller visible until the next replug.
    /// To use the controller directly, untick "Enable device hiding" in HidHide.
    /// </summary>
    public static class GpcHidHide
    {
        static readonly object gate = new object();

        /// <summary>Adds the device to HidHide's hidden list and turns hiding on. Returns a log line, or null.</summary>
        public static string HideFromGames(DS4Device device)
        {
            if (!Global.hidHideInstalled || device?.HidDevice == null)
                return null;

            string instanceId = Global.GetInstanceIdFromDevicePath(device.HidDevice.DevicePath);
            if (string.IsNullOrEmpty(instanceId))
                return null;

            lock (gate)
            {
                using (HidHideAPIDevice hidHide = new HidHideAPIDevice())
                {
                    if (!hidHide.IsOpen())
                        return "HidHide is installed but could not be opened; the real controller stays visible to games.";

                    // Inverse cloak means the user manages HidHide's app list the other way round; don't interfere.
                    if (hidHide.GetWhiteListInverseState())
                        return null;

                    List<string> hidden = hidHide.GetBlacklist();
                    bool listed = hidden.Any(h => string.Equals(h, instanceId, StringComparison.OrdinalIgnoreCase));
                    if (!listed)
                    {
                        hidden.Add(instanceId);
                        if (!hidHide.SetBlacklist(hidden))
                            return "Could not add the controller to HidHide's hidden list (DS4Windows may need to run as administrator).";
                    }

                    if (!hidHide.GetActiveState())
                    {
                        if (!hidHide.SetActiveState(true))
                            return "Could not turn HidHide hiding on (DS4Windows may need to run as administrator).";
                    }

                    return listed ? null :
                        $"{device.DisplayName} is now hidden from games (HidHide entry {instanceId}). Unplug and replug it, and restart a game or browser that already had it open.";
                }
            }
        }
    }
}
