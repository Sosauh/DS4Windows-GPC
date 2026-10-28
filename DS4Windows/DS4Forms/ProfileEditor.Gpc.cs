using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using DS4Windows;
using GpcScripts = DS4Windows.DS4Control.GpcScripting.GpcScripts;
using GpcScriptHost = DS4Windows.DS4Control.GpcScripting.GpcScriptHost;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>The "GPC script" box on the profile editor's Other tab.</summary>
    public partial class ProfileEditor
    {
        // Changes wait for Save/Apply, like every other profile setting.
        private string gpcPendingSource;
        private bool gpcPendingRemove;

        private void GpcResetPending()
        {
            gpcPendingSource = null;
            gpcPendingRemove = false;
            GpcRefreshStatus();
        }

        private void GpcRefreshStatus()
        {
            string name = currentProfile?.Name ?? profileNameTxt.Text;
            bool hasScript = !string.IsNullOrEmpty(name) && File.Exists(GpcScripts.ScriptPathFor(name));

            if (gpcPendingSource != null)
                gpcScriptStatusTxt.Text = $"{System.IO.Path.GetFileName(gpcPendingSource)} (used after Save)";
            else if (gpcPendingRemove)
                gpcScriptStatusTxt.Text = "None (script removed after Save)";
            else if (hasScript)
                gpcScriptStatusTxt.Text = $"{name}.gpc";
            else
                gpcScriptStatusTxt.Text = "None";

            gpcRemoveBtn.IsEnabled = gpcPendingSource != null || (hasScript && !gpcPendingRemove);
        }

        private void GpcChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "GPC script (*.gpc)|*.gpc|All files (*.*)|*.*";
            dialog.Title = "Select GPC script";
            if (dialog.ShowDialog() != true)
                return;

            string source;
            try
            {
                source = File.ReadAllText(dialog.FileName);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show($"Could not read the script: {ex.Message}", "GPC script",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check it now, so errors show here rather than as a silent no-op later.
            var result = global::Gpc.Engine.Compile.GpcCompiler.Compile(source);
            if (!result.Success)
            {
                var errors = result.Errors.Take(8).Select(d => d.ToString());
                MessageBox.Show($"{System.IO.Path.GetFileName(dialog.FileName)} has errors:\n\n{string.Join("\n", errors)}",
                    "GPC script", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            gpcPendingSource = dialog.FileName;
            gpcPendingRemove = false;
            GpcRefreshStatus();
        }

        private void GpcRemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            gpcPendingSource = null;
            gpcPendingRemove = true;
            GpcRefreshStatus();
        }

        private void GpcScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            // the test-profile slot has no running controller; show controller 1 instead
            int slot = deviceNum < Global.MAX_DS4_CONTROLLER_COUNT ? deviceNum : 0;
            new GpcScreenWindow(slot).Show();
        }

        /// <summary>Called once the profile has been saved under <paramref name="newName"/>.</summary>
        private void GpcApplyAfterSave(string oldName, string newName)
        {
            string dest = GpcScripts.ScriptPathFor(newName);
            string old = string.IsNullOrEmpty(oldName) ? null : GpcScripts.ScriptPathFor(oldName);
            bool renamed = old != null && !string.Equals(oldName, newName, StringComparison.Ordinal);
            string destMem = GpcScriptHost.MemoryPathFor(dest);
            string oldMem = old == null ? null : GpcScriptHost.MemoryPathFor(old);

            GpcScripts.Reset(); // saves script memory first, so the moves below carry the latest values

            try
            {
                if (gpcPendingRemove)
                {
                    if (File.Exists(dest)) File.Delete(dest);
                    if (File.Exists(destMem)) File.Delete(destMem);
                    if (renamed && File.Exists(old)) File.Delete(old);
                    if (renamed && File.Exists(oldMem)) File.Delete(oldMem);
                }
                else if (gpcPendingSource != null)
                {
                    // memory is kept, as on a Zen slot: get_pvar's range and default guard against another script's values
                    if (!string.Equals(System.IO.Path.GetFullPath(gpcPendingSource), System.IO.Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                        File.Copy(gpcPendingSource, dest, overwrite: true);
                    if (renamed && File.Exists(oldMem)) File.Move(oldMem, destMem, overwrite: true);
                }
                else if (renamed && File.Exists(old))
                {
                    File.Move(old, dest, overwrite: true); // the script and its memory follow a renamed profile
                    if (File.Exists(oldMem)) File.Move(oldMem, destMem, overwrite: true);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show($"Could not update the GPC script: {ex.Message}", "GPC script",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            GpcScripts.Reset(); // running controllers pick up the change on their next report
            GpcResetPending();
        }
    }
}
