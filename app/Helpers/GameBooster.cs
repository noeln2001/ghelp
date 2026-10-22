using System.Diagnostics;
using System.Runtime.InteropServices;
using GHelper.Mode;

namespace GHelper.Helpers
{
    public static class GameBooster
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        // Safe whitelist of system and critical processes that must NEVER be killed
        private static readonly HashSet<string> EssentialSystemProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "system", "idle", "smss", "csrss", "wininit", "winlogon", "services", "lsass",
            "fontdrvhost", "dwm", "svchost", "spoolsv", "taskhostw", "explorer", "sihost",
            "ctfmon", "shellexperiencehost", "searchhost", "startmenuexperiencehost",
            "securityhealthservice", "securityhealthsystray", "taskmgr", "audiodg",
            "ghelper", "antigravity", "code", "devenv", "cmd", "powershell", "pwsh", "conhost"
        };

        // Common heavy background applications and bloatware safe to close for gaming
        private static readonly HashSet<string> DefaultKillableProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "msedge", "firefox", "brave", "opera", "vivaldi",
            "discord", "spotify", "slack", "teams", "telegram", "whatsapp",
            "onedrive", "dropbox", "googledrivesync", "epicgameslauncher",
            "torrent", "utorrent", "qbittorrent", "notion", "obsidian",
            "cortana", "gamebar", "gamebarftserver", "yourphone", "phoneexperiencehost"
        };

        public static ulong GetAvailablePhysicalMemory()
        {
            var stat = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(stat))
            {
                return stat.ullAvailPhys;
            }
            return 0;
        }

        public static (int count, long bytes) ClearTempFiles()
        {
            int count = 0;
            long totalBytes = 0;

            List<string> tempPaths =
            [
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps")
            ];

            string winTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            if (Directory.Exists(winTemp)) tempPaths.Add(winTemp);

            foreach (var path in tempPaths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            long len = file.Length;
                            file.Delete();
                            count++;
                            totalBytes += len;
                        }
                        catch
                        {
                            // In-use or access denied files are safely skipped
                        }
                    }

                    foreach (var subDir in dirInfo.EnumerateDirectories())
                    {
                        try
                        {
                            subDir.Delete(true);
                        }
                        catch
                        {
                            // In-use folders are safely skipped
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"ClearTempFiles ({path}): {ex.Message}");
                }
            }

            return (count, totalBytes);
        }

        public static int KillBackgroundTasks()
        {
            int killed = 0;
            IntPtr fgHwnd = global::NativeMethods.GetForegroundWindow();
            uint fgPid = 0;
            if (fgHwnd != IntPtr.Zero)
            {
                global::NativeMethods.GetWindowThreadProcessId(fgHwnd, out fgPid);
            }

            // Custom user-defined process names
            var killSet = new HashSet<string>(DefaultKillableProcesses, StringComparer.OrdinalIgnoreCase);
            string custom = AppConfig.GetString("custom_kill_processes", "");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                var split = custom.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var name in split) killSet.Add(name);
            }

            Process[] processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    using (proc)
                    {
                        if (proc.Id <= 4 || proc.Id == fgPid || proc.Id == Environment.ProcessId)
                            continue;

                        string name = proc.ProcessName;
                        if (EssentialSystemProcesses.Contains(name))
                            continue;

                        if (killSet.Contains(name))
                        {
                            proc.Kill();
                            killed++;
                            Logger.WriteLine($"[GameBooster] Terminated background process: {name} (PID: {proc.Id})");
                        }
                    }
                }
                catch
                {
                    // Access denied or already exited
                }
            }

            return killed;
        }

        public static void TrimWorkingSets()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

            Process[] processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    using (proc)
                    {
                        if (proc.Id <= 4) continue;
                        EmptyWorkingSet(proc.Handle);
                    }
                }
                catch
                {
                    // Access denied on protected processes
                }
            }
        }

        public static void RunBoost(bool killTasks = true, bool clearTemp = true, bool trimRam = true, Action<string>? onComplete = null)
        {
            Task.Run(() =>
            {
                ulong memBefore = GetAvailablePhysicalMemory();
                int killed = 0;
                int tempFiles = 0;
                long tempBytes = 0;

                if (clearTemp)
                {
                    (tempFiles, tempBytes) = ClearTempFiles();
                }

                if (killTasks)
                {
                    killed = KillBackgroundTasks();
                }

                if (trimRam)
                {
                    TrimWorkingSets();
                }

                ulong memAfter = GetAvailablePhysicalMemory();
                long freedRamBytes = (long)(memAfter - memBefore);
                long freedRamMb = Math.Max(0, freedRamBytes / (1024 * 1024));
                long freedTempMb = tempBytes / (1024 * 1024);

                string summary = $"Boosted: {killed} tasks closed, {freedRamMb}MB RAM freed, {freedTempMb}MB temp cleaned";
                Logger.WriteLine("[GameBooster] " + summary);

                Program.toast.RunToast($"RAM Freed: +{freedRamMb}MB ({killed} apps closed)", ToastIcon.Charger);

                onComplete?.Invoke(summary);
            });
        }
    }
}
