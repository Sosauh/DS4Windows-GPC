using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>
    /// Decides when GPC scripts must switch off (DS4Windows itself is never stopped). Pure logic:
    /// feed it the running process names, it answers whether to stop and why.
    /// </summary>
    public sealed class GuardState
    {
        /// <summary>
        /// Process names (lower case, no ".exe") of anti-cheat clients and services. Not exhaustive:
        /// a game whose anti-cheat is not listed is not caught.
        /// </summary>
        public static readonly string[] KnownAntiCheat =
        {
            "easyanticheat", "easyanticheat_eos", "easyanticheat_eos_setup",   // Epic EAC (Fortnite, Apex, many others)
            "beservice", "beservice_x64",                                      // BattlEye
            "vgc", "vgtray",                                                   // Riot Vanguard (Valorant, LoL)
            "eaanticheat.gameservice", "eaanticheat.gameservicelauncher",      // EA Javelin
            "faceit", "faceitclient", "faceitservice",                         // FACEIT
            "esportal", "eseaclient",                                          // other CS:GO/CS2 platforms
            "gamemon", "gamemon64", "gameguard",                               // nProtect GameGuard
            "xigncode", "x3",                                                  // XIGNCODE3
            "mhyprot2",                                                        // miHoYo games
            "cod", "blackops6", "blackopscoldwar", "modernwarfare",            // Call of Duty (RICOCHET runs inside the game)
            "valorant", "valorant-win64-shipping",
        };

        readonly HashSet<string> blocked;

        public GuardState(IEnumerable<string> extraBlocked = null)
        {
            blocked = new HashSet<string>(KnownAntiCheat.Concat((extraBlocked ?? Enumerable.Empty<string>()).Select(Normalize)));
        }

        public static string Normalize(string processName)
        {
            string n = processName.Trim().ToLowerInvariant();
            return n.EndsWith(".exe") ? n.Substring(0, n.Length - 4) : n;
        }

        /// <summary>Returns a reason to shut down, or null to keep running.</summary>
        public string Check(IEnumerable<string> runningProcesses)
        {
            foreach (string raw in runningProcesses)
            {
                if (blocked.Contains(Normalize(raw))) return $"anti-cheat process '{raw}' is running";
            }
            return null;
        }
    }

    /// <summary>Polls running processes once a second on its own thread; fires each time a check fails.</summary>
    public sealed class AntiCheatGuard : IDisposable
    {
        readonly GuardState state;
        readonly Thread thread;
        volatile bool stop;

        /// <summary>Raised on the guard thread with the reason, while an anti-cheat process is present.</summary>
        public event Action<string> Tripped;

        public AntiCheatGuard(GuardState state)
        {
            this.state = state;
            thread = new Thread(Loop) { IsBackground = true, Name = "gpc-anti-cheat-guard" };
        }

        public string CheckNow() => state.Check(ProcessNames());

        public void Start() => thread.Start();

        static IEnumerable<string> ProcessNames()
        {
            foreach (Process p in Process.GetProcesses())
            {
                string name;
                try { name = p.ProcessName; }
                catch (InvalidOperationException) { continue; } // exited meanwhile
                finally { p.Dispose(); }
                yield return name;
            }
        }

        void Loop()
        {
            while (!stop)
            {
                string reason = state.Check(ProcessNames());
                if (reason != null) Tripped?.Invoke(reason);
                Thread.Sleep(1000);
            }
        }

        public void Dispose() => stop = true;
    }
}
