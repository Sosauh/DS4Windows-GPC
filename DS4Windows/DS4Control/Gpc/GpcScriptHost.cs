using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Gpc.Engine.Vm;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Runs one compiled script for one controller slot. Controller reports arrive every few ms;
    /// the script still runs on its own VM interval (10 ms by default, vm_tctrl changes it), the
    /// way a Zen does, and every report carries the output of the latest cycle.
    /// The script's persistent memory (set_pvar, set_bvar) is kept in &lt;script&gt;.mem next to it.
    /// </summary>
    public sealed class GpcScriptHost
    {
        const int MaxCatchUpCycles = 5;
        const int SaveDelayMs = 500;

        readonly Machine machine;
        readonly DS4State output = new DS4State();
        readonly object gate = new object();
        long nextCycle;
        bool started;
        int savedVersion;
        long changedAt;

        public string ScriptPath { get; }
        public string MemoryPath => MemoryPathFor(ScriptPath);
        public long FaultCount => machine.FaultCount;
        /// <summary>The script's 128x64 screen, shown by the GPC screen window.</summary>
        public global::Gpc.Engine.Runtime.Oled Oled => machine.Oled;

        public static string MemoryPathFor(string scriptPath) => scriptPath + ".mem";

        public GpcScriptHost(CompiledScript script, string path)
        {
            machine = new Machine(script);
            ScriptPath = path;
            try
            {
                if (File.Exists(MemoryPath) && !machine.LoadPersistent(File.ReadAllBytes(MemoryPath)))
                    AppLogger.LogToGui($"GPC: {Path.GetFileName(MemoryPath)} is not a saved memory file; starting with empty memory.", true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                AppLogger.LogToGui($"GPC: could not read {Path.GetFileName(MemoryPath)}: {e.Message}", true);
            }
            savedVersion = machine.PersistVersion;
        }

        /// <summary>Returns the state to send to the virtual pad.</summary>
        public DS4State Apply(DS4State input)
        {
            lock (gate)
            {
                long now = Stopwatch.GetTimestamp();
                if (!started)
                {
                    started = true;
                    nextCycle = now;
                }
                else if (now - nextCycle > Stopwatch.Frequency / 10)
                {
                    nextCycle = now; // reports stalled for 100 ms+: resume instead of replaying the gap
                }

                int ran = 0;
                while (now >= nextCycle && ran < MaxCatchUpCycles)
                {
                    GpcStateBridge.WriteInput(input, machine.Input);
                    int intervalMs = machine.RunCycle();
                    nextCycle += intervalMs * Stopwatch.Frequency / 1000;
                    ran++;
                }

                // save memory once the script has stopped changing it for a moment (menus change it per press)
                if (machine.PersistVersion != savedVersion)
                {
                    if (changedAt == 0)
                        changedAt = now;
                    else if (now - changedAt >= SaveDelayMs * Stopwatch.Frequency / 1000)
                    {
                        SnapshotLocked();
                        Task.Run(WritePending);
                    }
                }

                input.CopyTo(output);
                GpcStateBridge.ReadOutput(machine.Output, output);
                return output;
            }
        }

        /// <summary>Writes unsaved memory now (called when the script is unloaded).</summary>
        public void Flush()
        {
            lock (gate)
            {
                if (machine.PersistVersion != savedVersion)
                    SnapshotLocked();
            }
            WritePending(); // outside gate: WritePending takes writeGate, then gate
        }

        void SnapshotLocked()
        {
            pendingData = machine.SavePersistent();
            savedVersion = machine.PersistVersion;
            changedAt = 0;
        }

        byte[] pendingData;
        readonly object writeGate = new object();

        // Writes the newest snapshot; an older write that runs late finds nothing left to write.
        void WritePending()
        {
            lock (writeGate)
            {
                byte[] data;
                lock (gate)
                {
                    data = pendingData;
                    pendingData = null;
                }
                if (data == null) return;

                string path = MemoryPath;
                try
                {
                    string temp = path + ".tmp";
                    File.WriteAllBytes(temp, data);
                    File.Move(temp, path, overwrite: true);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    AppLogger.LogToGui($"GPC: could not save {Path.GetFileName(path)}: {e.Message}", true);
                }
            }
        }
    }
}
