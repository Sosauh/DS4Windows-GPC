using System.Diagnostics;
using Gpc.Engine.Vm;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Runs one compiled script for one controller slot. Controller reports arrive every few ms;
    /// the script still runs on its own VM interval (10 ms by default, vm_tctrl changes it), the
    /// way a Zen does, and every report carries the output of the latest cycle.
    /// </summary>
    public sealed class GpcScriptHost
    {
        const int MaxCatchUpCycles = 5;

        readonly Machine machine;
        readonly DS4State output = new DS4State();
        long nextCycle;
        bool started;

        public string ScriptPath { get; }
        public long FaultCount => machine.FaultCount;
        /// <summary>The script's 128x64 screen, shown by the GPC screen window.</summary>
        public global::Gpc.Engine.Runtime.Oled Oled => machine.Oled;

        public GpcScriptHost(CompiledScript script, string path)
        {
            machine = new Machine(script);
            ScriptPath = path;
        }

        /// <summary>Returns the state to send to the virtual pad.</summary>
        public DS4State Apply(DS4State input)
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

            input.CopyTo(output);
            GpcStateBridge.ReadOutput(machine.Output, output);
            return output;
        }
    }
}
