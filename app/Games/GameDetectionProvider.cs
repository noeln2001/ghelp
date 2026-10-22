using System.Text.RegularExpressions;
using GHelper.Helpers;
using Microsoft.Win32;

namespace GHelper.Games
{
    public class DetectedGame
    {
        public string Title { get; set; } = "";
        public string ExecutablePath { get; set; } = "";
        public string ExecutableName { get; set; } = "";
        public string Launcher { get; set; } = "Steam"; // Steam, Epic, Xbox, Custom
    }

    public static class GameDetectionProvider
    {
        public static List<DetectedGame> ScanInstalledGames()
        {
            var games = new List<DetectedGame>();

            ScanSteamGames(games);
            ScanEpicGames(games);
            ScanXboxGames(games);

            return games.DistinctBy(g => g.ExecutableName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void ScanSteamGames(List<DetectedGame> list)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                string? steamPath = key?.GetValue("SteamPath")?.ToString();
                if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath)) return;

                List<string> libraryPaths = [Path.Combine(steamPath, "steamapps")];
                string vdfFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

                if (File.Exists(vdfFile))
                {
                    string vdfContent = File.ReadAllText(vdfFile);
                    var matches = Regex.Matches(vdfContent, @"""path""\s+""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        string p = m.Groups[1].Value.Replace(@"\\", @"\");
                        string appsPath = Path.Combine(p, "steamapps");
                        if (Directory.Exists(appsPath) && !libraryPaths.Contains(appsPath))
                        {
                            libraryPaths.Add(appsPath);
                        }
                    }
                }

                foreach (var lib in libraryPaths)
                {
                    if (!Directory.Exists(lib)) continue;
                    foreach (var acf in Directory.GetFiles(lib, "appmanifest_*.acf"))
                    {
                        try
                        {
                            string text = File.ReadAllText(acf);
                            string name = Regex.Match(text, @"""name""\s+""([^""]+)""").Groups[1].Value;
                            string installDir = Regex.Match(text, @"""installdir""\s+""([^""]+)""").Groups[1].Value;

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(installDir))
                            {
                                string fullDir = Path.Combine(lib, "common", installDir);
                                if (Directory.Exists(fullDir))
                                {
                                    var exe = Directory.GetFiles(fullDir, "*.exe", SearchOption.AllDirectories)
                                                       .Where(f => !f.Contains("crash", StringComparison.OrdinalIgnoreCase) && !f.Contains("unins", StringComparison.OrdinalIgnoreCase))
                                                       .FirstOrDefault();

                                    list.Add(new DetectedGame
                                    {
                                        Title = name,
                                        ExecutablePath = exe ?? "",
                                        ExecutableName = exe != null ? Path.GetFileNameWithoutExtension(exe).ToLowerInvariant() : installDir.ToLowerInvariant(),
                                        Launcher = "Steam"
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[GameDetection] Steam scan error: {ex.Message}");
            }
        }

        private static void ScanEpicGames(List<DetectedGame> list)
        {
            try
            {
                string epicManifests = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
                if (!Directory.Exists(epicManifests)) return;

                foreach (var item in Directory.GetFiles(epicManifests, "*.item"))
                {
                    try
                    {
                        string content = File.ReadAllText(item);
                        string title = Regex.Match(content, @"""DisplayName""\s*:\s*""([^""]+)""").Groups[1].Value;
                        string exe = Regex.Match(content, @"""LaunchExecutable""\s*:\s*""([^""]+)""").Groups[1].Value;
                        string location = Regex.Match(content, @"""InstallLocation""\s*:\s*""([^""]+)""").Groups[1].Value;

                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(exe))
                        {
                            string exeName = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                            string fullPath = !string.IsNullOrEmpty(location) ? Path.Combine(location, exe) : exe;

                            list.Add(new DetectedGame
                            {
                                Title = title,
                                ExecutablePath = fullPath,
                                ExecutableName = exeName,
                                Launcher = "Epic Games"
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[GameDetection] Epic scan error: {ex.Message}");
            }
        }

        private static void ScanXboxGames(List<DetectedGame> list)
        {
            try
            {
                string[] xboxDirs = [@"C:\XboxGames", @"D:\XboxGames", @"E:\XboxGames"];
                foreach (var xDir in xboxDirs)
                {
                    if (!Directory.Exists(xDir)) continue;
                    foreach (var gameFolder in Directory.GetDirectories(xDir))
                    {
                        string folderName = Path.GetFileName(gameFolder);
                        var exe = Directory.GetFiles(gameFolder, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                        if (exe != null)
                        {
                            list.Add(new DetectedGame
                            {
                                Title = folderName,
                                ExecutablePath = exe,
                                ExecutableName = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant(),
                                Launcher = "Xbox PC"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[GameDetection] Xbox scan error: {ex.Message}");
            }
        }
    }
}
