using System;
using System.Collections.Generic;
using System.Linq;
using DS4WinWPF.DS4Control;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Hides connected controllers from games through HidHide, so games see only the virtual pad.
    /// DS4Windows stays able to read them because it whitelists itself in HidHide at startup.
    /// Only entries added here are removed again when the service stops; the user's own HidHide
    /// configuration is left alone.
    /// </summary>
    public static class GpcHidHide
    {
        static readonly object gate = new object();
        static readonly HashSet<string> addedInstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static bool turnedHidingOn;

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
                        addedInstances.Add(instanceId);
                    }

                    if (!hidHide.GetActiveState())
                    {
                        if (!hidHide.SetActiveState(true))
                            return "Could not turn HidHide hiding on (DS4Windows may need to run as administrator).";
                        turnedHidingOn = true;
                    }

                    return listed ? null :
                        $"{device.DisplayName} is now hidden from games (HidHide). Restart a game or browser that already had it open.";
                }
            }
        }

        /// <summary>Undoes only what <see cref="HideFromGames"/> added, so games see the real controller again.</summary>
        public static void RestoreAll(Action<string> log)
        {
            if (!Global.hidHideInstalled)
                return;

            lock (gate)
            {
                if (addedInstances.Count == 0 && !turnedHidingOn)
                    return;

                using (HidHideAPIDevice hidHide = new HidHideAPIDevice())
                {
                    if (!hidHide.IsOpen())
                        return;

                    List<string> hidden = hidHide.GetBlacklist();
                    hidden.RemoveAll(h => addedInstances.Contains(h));
                    hidHide.SetBlacklist(hidden);
                    if (turnedHidingOn && hidden.Count == 0)
                        hidHide.SetActiveState(false);
                }

                log?.Invoke("Controllers are visible to games again (HidHide entries added by DS4Windows removed).");
                addedInstances.Clear();
                turnedHidingOn = false;
            }
        }
    }
}
