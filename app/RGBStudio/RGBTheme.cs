using System.Drawing;
using System.Text.Json;
using GHelper.Helpers;

namespace GHelper.RGBStudio
{
    public class RGBTheme
    {
        public string Name { get; set; } = "Cyberpunk";
        public string Author { get; set; } = "G-Helper";
        public string Description { get; set; } = "";
        public string EffectId { get; set; } = "CyberpunkMatrix";
        public string PrimaryHex { get; set; } = "#00FFC8";
        public string SecondaryHex { get; set; } = "#FF007F";
        public string AccentHex { get; set; } = "#FFE600";
        public string BackgroundHex { get; set; } = "#0A0A14";
        public int Brightness { get; set; } = 100;
        public int Speed { get; set; } = 60;
        public int Direction { get; set; } = 0;
        public Dictionary<int, string> PerKeyHexMap { get; set; } = new();

        public Color GetPrimaryColor() => RGBColorUtilities.HexToColor(PrimaryHex);
        public Color GetSecondaryColor() => RGBColorUtilities.HexToColor(SecondaryHex);
        public Color GetAccentColor() => RGBColorUtilities.HexToColor(AccentHex);
        public Color GetBackgroundColor() => RGBColorUtilities.HexToColor(BackgroundHex);

        public RGBEffectConfig ToConfig()
        {
            var config = new RGBEffectConfig
            {
                EffectId = EffectId,
                PrimaryColor = GetPrimaryColor(),
                SecondaryColor = GetSecondaryColor(),
                AccentColor = GetAccentColor(),
                BackgroundColor = GetBackgroundColor(),
                Brightness = Brightness,
                Speed = Speed,
                Direction = (EffectDirection)Direction
            };

            foreach (var kvp in PerKeyHexMap)
            {
                config.PerKeyColors[kvp.Key] = RGBColorUtilities.HexToColor(kvp.Value);
            }

            return config;
        }

        public static RGBTheme FromConfig(string name, RGBEffectConfig config)
        {
            var theme = new RGBTheme
            {
                Name = name,
                EffectId = config.EffectId,
                PrimaryHex = RGBColorUtilities.ColorToHex(config.PrimaryColor),
                SecondaryHex = RGBColorUtilities.ColorToHex(config.SecondaryColor),
                AccentHex = RGBColorUtilities.ColorToHex(config.AccentColor),
                BackgroundHex = RGBColorUtilities.ColorToHex(config.BackgroundColor),
                Brightness = config.Brightness,
                Speed = config.Speed,
                Direction = (int)config.Direction
            };

            foreach (var kvp in config.PerKeyColors)
            {
                theme.PerKeyHexMap[kvp.Key] = RGBColorUtilities.ColorToHex(kvp.Value);
            }

            return theme;
        }
    }

    public static class RGBThemeManager
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public static readonly List<RGBTheme> BuiltInThemes =
        [
            new()
            {
                Name = "Cyberpunk",
                Description = "High-tech synthwave neon cyan, hot pink and yellow accents",
                EffectId = "CyberpunkMatrix",
                PrimaryHex = "#00FFC8",
                SecondaryHex = "#FF007F",
                AccentHex = "#FFE600",
                BackgroundHex = "#050510",
                Brightness = 100,
                Speed = 65
            },
            new()
            {
                Name = "Neon Purple",
                Description = "Deep violet and electric ultraviolet pulse",
                EffectId = "Breathing",
                PrimaryHex = "#9900FF",
                SecondaryHex = "#FF00CC",
                AccentHex = "#6600CC",
                BackgroundHex = "#100020",
                Brightness = 90,
                Speed = 45
            },
            new()
            {
                Name = "Ice Blue",
                Description = "Crisp arctic glacial glow with frosty highlights",
                EffectId = "Aurora",
                PrimaryHex = "#00D4FF",
                SecondaryHex = "#FFFFFF",
                AccentHex = "#0077B6",
                BackgroundHex = "#001220",
                Brightness = 95,
                Speed = 50
            },
            new()
            {
                Name = "Red & Black",
                Description = "Aggressive ROG Strix crimson fire aesthetics",
                EffectId = "Fire",
                PrimaryHex = "#FF001E",
                SecondaryHex = "#FF5500",
                AccentHex = "#990000",
                BackgroundHex = "#000000",
                Brightness = 100,
                Speed = 55
            },
            new()
            {
                Name = "Minimal White",
                Description = "Clean studio white backlight for productivity",
                EffectId = "StaticColor",
                PrimaryHex = "#FFFFFF",
                SecondaryHex = "#D0D0D0",
                AccentHex = "#FFFFFF",
                BackgroundHex = "#000000",
                Brightness = 80,
                Speed = 0
            },
            new()
            {
                Name = "Rainbow Wave",
                Description = "Dynamic flowing full-spectrum wave across the laptop and mouse",
                EffectId = "RainbowWave",
                PrimaryHex = "#FF0000",
                SecondaryHex = "#00FF00",
                AccentHex = "#0000FF",
                BackgroundHex = "#000000",
                Brightness = 100,
                Speed = 50
            },
            new()
            {
                Name = "Stealth Mode",
                Description = "Completely turns off all lighting for zero distraction and maximum battery",
                EffectId = "StaticColor",
                PrimaryHex = "#000000",
                SecondaryHex = "#000000",
                AccentHex = "#000000",
                BackgroundHex = "#000000",
                Brightness = 0,
                Speed = 0
            },
            new()
            {
                Name = "FPS Competitive",
                Description = "Illuminates WASD, 1-4, Shift and Space in bright gold, surrounding keys dim",
                EffectId = "StaticColor",
                PrimaryHex = "#333333",
                SecondaryHex = "#00FF00",
                AccentHex = "#FFD700",
                BackgroundHex = "#000000",
                Brightness = 100,
                Speed = 0
            },
            new()
            {
                Name = "Story Gaming",
                Description = "Immersive atmospheric deep warm amber and gold",
                EffectId = "Breathing",
                PrimaryHex = "#FF8800",
                SecondaryHex = "#CC4400",
                AccentHex = "#FFAA33",
                BackgroundHex = "#220A00",
                Brightness = 85,
                Speed = 35
            }
        ];

        public static bool ExportThemeToFile(RGBTheme theme, string filePath)
        {
            try
            {
                string json = JsonSerializer.Serialize(theme, JsonOpts);
                File.WriteAllText(filePath, json);
                Logger.WriteLine($"[RGBTheme] Theme '{theme.Name}' exported to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[RGBTheme] Export error: {ex.Message}");
                return false;
            }
        }

        public static RGBTheme? ImportThemeFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 1024 * 512) // 512 KB limit to prevent oversized input
                {
                    Logger.WriteLine("[RGBTheme] Import rejected: file exceeds safety limit of 512KB");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var theme = JsonSerializer.Deserialize<RGBTheme>(json);
                if (theme != null && !string.IsNullOrWhiteSpace(theme.Name))
                {
                    Logger.WriteLine($"[RGBTheme] Successfully imported theme '{theme.Name}'");
                    return theme;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[RGBTheme] Import error: {ex.Message}");
            }
            return null;
        }
    }
}
