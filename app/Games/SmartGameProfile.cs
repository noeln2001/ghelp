using System.Text.Json;
using GHelper.Helpers;
using GHelper.Mode;
using GHelper.RGBStudio;

namespace GHelper.Games
{
    public class SmartGameProfile
    {
        public string Title { get; set; } = "Game";
        public string ExecutableName { get; set; } = "";
        public int PerformanceMode { get; set; } = AsusACPI.PerformanceTurbo;
        public string RgbThemeName { get; set; } = "Cyberpunk";
        public int TargetRefreshRate { get; set; } = 240; // 240Hz for Strix G16
        public bool EnableGameBooster { get; set; } = true;
    }

    public static class SmartGameProfileManager
    {
        private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GHelper", "game_profiles.json");
        private static Dictionary<string, SmartGameProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

        private static int _savedMode = -1;
        private static bool _profileActive = false;
        private static string _activeGame = "";

        static SmartGameProfileManager()
        {
            LoadProfiles();
        }

        public static IReadOnlyDictionary<string, SmartGameProfile> Profiles => _profiles;

        public static void LoadProfiles()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var list = JsonSerializer.Deserialize<List<SmartGameProfile>>(json);
                    if (list != null)
                    {
                        _profiles = list.ToDictionary(p => p.ExecutableName, StringComparer.OrdinalIgnoreCase);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[SmartGameProfiles] Load error: {ex.Message}");
            }

            // Default initial profile for demonstration
            _profiles = new Dictionary<string, SmartGameProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["cyberpunk2077"] = new SmartGameProfile { Title = "Cyberpunk 2077", ExecutableName = "cyberpunk2077", PerformanceMode = AsusACPI.PerformanceTurbo, RgbThemeName = "Cyberpunk", TargetRefreshRate = 240, EnableGameBooster = true },
                ["cs2"] = new SmartGameProfile { Title = "Counter-Strike 2", ExecutableName = "cs2", PerformanceMode = AsusACPI.PerformanceTurbo, RgbThemeName = "FPS Competitive", TargetRefreshRate = 240, EnableGameBooster = true },
                ["witcher3"] = new SmartGameProfile { Title = "The Witcher 3", ExecutableName = "witcher3", PerformanceMode = AsusACPI.PerformanceTurbo, RgbThemeName = "Story Gaming", TargetRefreshRate = 240, EnableGameBooster = true }
            };
            SaveProfiles();
        }

        public static void SaveProfiles()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                string json = JsonSerializer.Serialize(_profiles.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[SmartGameProfiles] Save error: {ex.Message}");
            }
        }

        public static void SetProfile(SmartGameProfile profile)
        {
            _profiles[profile.ExecutableName] = profile;
            SaveProfiles();
        }

        public static void RemoveProfile(string executableName)
        {
            if (_profiles.Remove(executableName))
            {
                SaveProfiles();
            }
        }

        public static bool CheckAndApply(string procName)
        {
            if (_profiles.TryGetValue(procName, out var profile))
            {
                if (!_profileActive)
                {
                    _profileActive = true;
                    _activeGame = procName;
                    _savedMode = Modes.GetCurrent();

                    Logger.WriteLine($"[SmartGameProfiles] Activating profile '{profile.Title}' (Mode: {Modes.GetName(profile.PerformanceMode)}, RGB: {profile.RgbThemeName})");

                    // 1. Performance mode
                    Program.modeControl.SetPerformanceMode(profile.PerformanceMode, false);

                    // 2. RGB theme
                    var theme = RGBThemeManager.BuiltInThemes.FirstOrDefault(t => t.Name.Equals(profile.RgbThemeName, StringComparison.OrdinalIgnoreCase));
                    if (theme != null)
                    {
                        RGBDeviceManager.Instance.ApplyTheme(theme);
                    }

                    // 3. Screen rate
                    if (profile.TargetRefreshRate > 0)
                    {
                        Display.ScreenControl.SetScreen(profile.TargetRefreshRate, 1);
                    }

                    // 4. Game Booster
                    if (profile.EnableGameBooster)
                    {
                        GameBooster.RunBoost(killTasks: true, clearTemp: true, trimRam: true);
                    }

                    Program.toast.RunToast($"Profile: {profile.Title} Active", ToastIcon.Charger);
                }
                return true;
            }
            return false;
        }

        public static void RestoreIfExited(string currentProcName)
        {
            if (_profileActive && !string.Equals(currentProcName, _activeGame, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteLine($"[SmartGameProfiles] Game '{_activeGame}' exited, restoring mode {Modes.GetName(_savedMode)}");
                if (_savedMode >= 0 && Modes.Exists(_savedMode))
                {
                    Program.modeControl.SetPerformanceMode(_savedMode, false);
                }

                _profileActive = false;
                _activeGame = "";
            }
        }
    }
}
