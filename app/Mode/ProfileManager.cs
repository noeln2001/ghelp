using System.Text.Json;
using GHelper.Helpers;

namespace GHelper.Mode
{
    public class FanPowerProfile
    {
        public string? ModeName { get; set; }
        public int ModeId { get; set; }
        public Dictionary<string, int> PowerLimits { get; set; } = new();
        public Dictionary<string, string> FanCurves { get; set; } = new();
        public Dictionary<string, int> GpuSettings { get; set; } = new();
        public Dictionary<string, int> UndervoltSettings { get; set; } = new();
        public Dictionary<string, int> Flags { get; set; } = new();
    }

    public static class ProfileManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static bool ExportProfile(int mode, string filePath)
        {
            try
            {
                var profile = new FanPowerProfile
                {
                    ModeName = Modes.GetName(mode),
                    ModeId = mode
                };

                // Power limits
                string[] powerKeys = ["limit_total", "limit_slow", "limit_fast", "limit_cpu", "limit_crossload", "limit_gpucpu", "limit_cputemp"];
                foreach (var key in powerKeys)
                {
                    int val = AppConfig.GetMode(key);
                    if (val != -1) profile.PowerLimits[key] = val;
                }

                // Fan curves
                AsusFan[] fans = [AsusFan.CPU, AsusFan.GPU, AsusFan.Mid, AsusFan.XGM];
                foreach (var fan in fans)
                {
                    byte[] curve = AppConfig.GetFanConfig(fan);
                    if (curve != null && curve.Length > 0)
                    {
                        profile.FanCurves[fan.ToString()] = BitConverter.ToString(curve);
                    }
                }

                // GPU settings
                string[] gpuKeys = ["gpu_core", "gpu_memory", "gpu_boost", "gpu_temp", "gpu_power", "gpu_clock_limit"];
                foreach (var key in gpuKeys)
                {
                    int val = AppConfig.GetMode(key);
                    if (val != -1) profile.GpuSettings[key] = val;
                }

                // Undervolt
                string[] uvKeys = ["cpu_uv", "igpu_uv", "cpu_temp", "auto_uv"];
                foreach (var key in uvKeys)
                {
                    int val = AppConfig.GetMode(key);
                    if (val != -1) profile.UndervoltSettings[key] = val;
                }

                // Flags
                string[] flagKeys = ["auto_apply", "auto_apply_power", "auto_boost", "hysteresis_up", "hysteresis_down"];
                foreach (var key in flagKeys)
                {
                    int val = AppConfig.GetMode(key);
                    if (val != -1) profile.Flags[key] = val;
                }

                string json = JsonSerializer.Serialize(profile, JsonOptions);
                File.WriteAllText(filePath, json);
                Logger.WriteLine($"Profile exported to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Profile export failed: {ex.Message}");
                return false;
            }
        }

        public static bool ImportProfile(int mode, string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                string json = File.ReadAllText(filePath);
                var profile = JsonSerializer.Deserialize<FanPowerProfile>(json);
                if (profile == null) return false;

                // Power limits
                foreach (var kvp in profile.PowerLimits)
                {
                    AppConfig.SetMode(kvp.Key, kvp.Value);
                }

                // Fan curves
                foreach (var kvp in profile.FanCurves)
                {
                    if (Enum.TryParse<AsusFan>(kvp.Key, true, out var fan))
                    {
                        byte[] curve = AppConfig.StringToBytes(kvp.Value);
                        AppConfig.SetFanConfig(fan, curve);
                    }
                }

                // GPU settings
                foreach (var kvp in profile.GpuSettings)
                {
                    AppConfig.SetMode(kvp.Key, kvp.Value);
                }

                // Undervolt
                foreach (var kvp in profile.UndervoltSettings)
                {
                    AppConfig.SetMode(kvp.Key, kvp.Value);
                }

                // Flags
                foreach (var kvp in profile.Flags)
                {
                    AppConfig.SetMode(kvp.Key, kvp.Value);
                }

                Logger.WriteLine($"Profile imported from {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Profile import failed: {ex.Message}");
                return false;
            }
        }
    }
}
