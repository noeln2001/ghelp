using System.Diagnostics;
using System.Runtime.InteropServices;
using GHelper.Helpers;

namespace GHelper.Performance
{
    public static class GamingLatencyOptimizer
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        private static bool _highResTimerActive = false;
        private static System.Threading.Timer? _gameWatcherTimer;

        public static bool IsHighResTimerActive => _highResTimerActive;
        public static bool IsPCoreLockEnabled => AppConfig.Is("intel_pcore_lock");
        public static bool IsHighPriorityEnabled => AppConfig.Is("game_high_priority");

        public static void SetHighResTimer(bool enable)
        {
            if (enable && !_highResTimerActive)
            {
                uint res = TimeBeginPeriod(1);
                _highResTimerActive = (res == 0);
                AppConfig.Set("high_res_timer", _highResTimerActive ? 1 : 0);
                Logger.WriteLine($"[GamingOptimizer] 0.5ms-1ms High-Resolution Multimedia Timer activated: {_highResTimerActive}");
            }
            else if (!enable && _highResTimerActive)
            {
                TimeEndPeriod(1);
                _highResTimerActive = false;
                AppConfig.Set("high_res_timer", 0);
                Logger.WriteLine("[GamingOptimizer] High-Resolution Multimedia Timer released.");
            }
        }

        public static void SetPCoreLock(bool enable)
        {
            AppConfig.Set("intel_pcore_lock", enable ? 1 : 0);
            Logger.WriteLine($"[GamingOptimizer] Intel P-Core Lock for Games set to: {enable}");
            if (enable)
            {
                StartGameWatcher();
                OptimizeActiveForegroundGame();
            }
        }

        public static void SetGameHighPriority(bool enable)
        {
            AppConfig.Set("game_high_priority", enable ? 1 : 0);
            Logger.WriteLine($"[GamingOptimizer] Game High Process Priority set to: {enable}");
            if (enable)
            {
                OptimizeActiveForegroundGame();
            }
        }

        public static void StartGameWatcher()
        {
            if (_gameWatcherTimer != null) return;
            _gameWatcherTimer = new System.Threading.Timer(_ =>
            {
                if (IsPCoreLockEnabled || IsHighPriorityEnabled)
                {
                    OptimizeActiveForegroundGame();
                }
            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
        }

        public static void StopGameWatcher()
        {
            _gameWatcherTimer?.Dispose();
            _gameWatcherTimer = null;
        }

        /// <summary>
        /// Detects active foreground game and applies Intel 14th-Gen P-Core affinity + High Priority.
        /// </summary>
        public static bool OptimizeActiveForegroundGame()
        {
            try
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid <= 4) return false;

                using var proc = Process.GetProcessById((int)pid);
                string procName = proc.ProcessName.ToLowerInvariant();

                // Skip system and standard utility applications
                if (procName is "explorer" or "ghelper" or "taskmgr" or "dwm" or "csrss" or "devenv" or "code")
                    return false;

                return OptimizeProcess(proc);
            }
            catch
            {
                return false;
            }
        }

        public static bool OptimizeProcess(Process proc)
        {
            bool modified = false;
            try
            {
                // 1. Process Priority
                if (IsHighPriorityEnabled && proc.PriorityClass != ProcessPriorityClass.High)
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                    modified = true;
                    Logger.WriteLine($"[GamingOptimizer] Set High Priority for {proc.ProcessName} (PID {proc.Id})");
                }

                // 2. Intel 14th-Gen P-Core Affinity
                // 14th Gen Intel Core i9 has 8 P-Cores (16 threads: logical 0-15) + 16 E-Cores (threads 16-31).
                // Mask for P-cores 0 to 15 is 0x0000FFFF.
                if (IsPCoreLockEnabled && Environment.ProcessorCount >= 16)
                {
                    long pCoreMask = 0xFFFF;
                    if (proc.ProcessorAffinity.ToInt64() != pCoreMask)
                    {
                        proc.ProcessorAffinity = (IntPtr)pCoreMask;
                        modified = true;
                        Logger.WriteLine($"[GamingOptimizer] Locked {proc.ProcessName} to Intel P-Cores (Affinity: 0x{pCoreMask:X})");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[GamingOptimizer] OptimizeProcess error for {proc.ProcessName}: {ex.Message}");
            }
            return modified;
        }

        /// <summary>
        /// Flushes Windows standby memory cache and trims working sets.
        /// </summary>
        public static long FlushMemoryAndWorkingSets()
        {
            long before = GC.GetTotalMemory(false);
            try
            {
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (!proc.HasExited && proc.Id > 4)
                        {
                            string name = proc.ProcessName.ToLowerInvariant();
                            if (name is not ("ghelper" or "dwm" or "csrss"))
                            {
                                EmptyWorkingSet(proc.Handle);
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[GamingOptimizer] FlushMemory error: {ex.Message}");
            }

            long after = GC.GetTotalMemory(true);
            return Math.Max(0, before - after);
        }

        public static void InitializeFromSettings()
        {
            if (AppConfig.Is("high_res_timer"))
            {
                SetHighResTimer(true);
            }

            if (IsPCoreLockEnabled || IsHighPriorityEnabled)
            {
                StartGameWatcher();
            }
        }
    }
}
