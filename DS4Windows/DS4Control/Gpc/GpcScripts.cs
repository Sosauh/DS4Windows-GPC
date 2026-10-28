using System;
using System.IO;
using Gpc.Engine;
using Gpc.Engine.Compile;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Per-slot GPC scripts. A profile named "Bully" runs Profiles\Bully.gpc when that file exists,
    /// so Auto Profiles decide when a script is active.
    /// </summary>
    public static class GpcScripts
    {
        static readonly GpcScriptHost[] hosts = new GpcScriptHost[Global.MAX_DS4_CONTROLLER_COUNT];
        static readonly string[] loadedFor = new string[Global.MAX_DS4_CONTROLLER_COUNT];
        static readonly object gate = new object();

        public static string ScriptPathFor(string profileName) =>
            Path.Combine(Global.appdatapath, "Profiles", profileName + ".gpc");

        /// <summary>Holds the name of the file the user picked; the script itself is stored as &lt;profile&gt;.gpc.</summary>
        public static string NamePathFor(string scriptPath) => scriptPath + ".name";

        /// <summary>The picked file's name (e.g. "wolf-warrior-retribution.gpc"), or the stored file's name.</summary>
        public static string DisplayNameFor(string scriptPath)
        {
            try
            {
                string n = File.Exists(NamePathFor(scriptPath)) ? File.ReadAllText(NamePathFor(scriptPath)).Trim() : "";
                if (n.Length > 0) return n;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
            return Path.GetFileName(scriptPath);
        }

        /// <summary>The running script for a controller slot, or null.</summary>
        public static GpcScriptHost HostFor(int ind) =>
            ind >= 0 && ind < hosts.Length ? hosts[ind] : null;

        /// <summary>Forget loaded scripts so edited .gpc files are picked up (called when the service starts).</summary>
        public static void Reset()
        {
            lock (gate)
            {
                foreach (var h in hosts) h?.Flush();
                Array.Clear(hosts, 0, hosts.Length);
                Array.Clear(loadedFor, 0, loadedFor.Length);
            }
        }

        /// <summary>Called from the report path. Returns the state to send: the script's output, or the input unchanged.</summary>
        public static DS4State Apply(int ind, DS4State state)
        {
            if (ind < 0 || ind >= hosts.Length) return state;

            string profile = Global.useTempProfile[ind] ? Global.tempprofilename[ind] : Global.ProfilePath[ind];
            if (!string.Equals(profile, loadedFor[ind], StringComparison.Ordinal))
                Load(ind, profile ?? string.Empty);

            GpcScriptHost host = hosts[ind];
            return host == null ? state : host.Apply(state);
        }

        static void Load(int ind, string profile)
        {
            lock (gate)
            {
                loadedFor[ind] = profile;
                hosts[ind]?.Flush();
                hosts[ind] = null;
                if (profile.Length == 0) return;

                string path = ScriptPathFor(profile);
                if (!File.Exists(path)) return;

                CompileResult result;
                try
                {
                    result = GpcCompiler.Compile(File.ReadAllText(path));
                }
                catch (IOException e)
                {
                    AppLogger.LogToGui($"GPC: could not read {path}: {e.Message}", true);
                    return;
                }

                string shown = DisplayNameFor(path);
                foreach (var d in result.Diagnostics)
                    AppLogger.LogToGui($"GPC {shown}:{d}", d.Severity == Severity.Error);

                if (!result.Success)
                {
                    AppLogger.LogToGui($"GPC: {shown} has errors; controller {ind + 1} runs without a script.", true);
                    return;
                }
                hosts[ind] = new GpcScriptHost(result.Script, path);
                AppLogger.LogToGui($"GPC: controller {ind + 1} running {shown} (profile \"{profile}\")", false);
            }
        }
    }
}
