using System.Diagnostics;
using GHelper.Games;
using GHelper.Helpers;

namespace GHelper.Mode
{
    public class AppProfileControl
    {
        private static System.Timers.Timer? _timer;
        private static int _previousMode = -1;
        private static bool _inGameMode = false;
        private static string _currentGame = "";

        private static readonly HashSet<string> DefaultGameProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "cyberpunk2077", "witcher3", "starfield", "eldenring", "forzaborizon5", "forza",
            "cs2", "csgo", "valorant-win64-shipping", "overwatch", "cod", "gta5", "rdr2",
            "bg3", "bg3_dx11", "helldivers2", "apexlegends", "r5apex", "destiny2", "dota2",
            "league of legends", "genshinimpact", "honkaistarrail", "zenlesszonezero",
            "fortniteclient-win64-shipping", "unrealeditor", "unity", "blender", "timespy", "3dmark"
        };

        public static void Init()
        {
            if (_timer != null) return;

            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!AppConfig.Is("auto_game_mode"))
            {
                if (_inGameMode) ResetGameMode();
                return;
            }

            try
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid <= 4) return;

                using var proc = Process.GetProcessById((int)pid);
                string procName = proc.ProcessName.ToLowerInvariant();

                // Ignore desktop, explorer, ghelper, taskmgr
                if (procName is "explorer" or "ghelper" or "taskmgr" or "searchhost" or "lockapp" or "shellexperiencehost")
                    return;

                bool isGame = IsGameProcess(procName);

                if (isGame && !_inGameMode)
                {
                    _previousMode = Modes.GetCurrent();
                    _currentGame = procName;
                    _inGameMode = true;

                    // 1. Check if configured in SmartGameProfileManager
                    if (SmartGameProfileManager.CheckAndApply(procName))
                    {
                        Logger.WriteLine($"[AutoGame] Smart Game Profile applied for '{procName}'");
                    }
                    else
                    {
                        int targetMode = AppConfig.Get("game_profile_mode", AsusACPI.PerformanceTurbo);
                        if (targetMode != _previousMode)
                        {
                            Logger.WriteLine($"[AutoGame] Game detected '{procName}', switching to {Modes.GetName(targetMode)}");
                            Program.modeControl.SetPerformanceMode(targetMode, false);
                            Program.toast.RunToast($"Game Mode: {proc.ProcessName}", ToastIcon.Charger);
                        }

                        if (AppConfig.Is("auto_game_boost"))
                        {
                            GameBooster.RunBoost(killTasks: true, clearTemp: true, trimRam: true);
                        }
                    }
                }
                else if (!isGame && _inGameMode)
                {
                    ResetGameMode();
                }
            }
            catch
            {
                // Process may exit during query
            }
        }

        private static void ResetGameMode()
        {
            if (!_inGameMode) return;
            _inGameMode = false;

            SmartGameProfileManager.RestoreIfExited("");

            if (_previousMode != -1 && Modes.Exists(_previousMode) && _previousMode != Modes.GetCurrent())
            {
                Logger.WriteLine($"[AutoGame] Game exited '{_currentGame}', restoring {Modes.GetName(_previousMode)}");
                Program.modeControl.SetPerformanceMode(_previousMode, false);
                Program.toast.RunToast($"Restored: {Modes.GetName(_previousMode)}", ToastIcon.Charger);
            }
            _currentGame = "";
        }

        private static bool IsGameProcess(string name)
        {
            if (SmartGameProfileManager.Profiles.ContainsKey(name)) return true;
            if (DefaultGameProcesses.Contains(name)) return true;

            string custom = AppConfig.GetString("custom_game_processes", "");
            if (!string.IsNullOrEmpty(custom))
            {
                var list = custom.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var item in list)
                {
                    if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
    }
}
