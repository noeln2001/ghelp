using Microsoft.Win32;
using GHelper.Helpers;

namespace GHelper.Network
{
    public static class GamingNetworkOptimizer
    {
        private const string SystemProfilePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string GamesTaskPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        private const string TcpInterfacesPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        public static bool IsNetworkOptimized()
        {
            return AppConfig.Is("gaming_network_opt");
        }

        public static bool ApplyGamingNetworkOptimizations()
        {
            try
            {
                // 1. Multimedia System Profile (Disable network throttling & maximize system responsiveness)
                using (var sysProfile = Registry.LocalMachine.OpenSubKey(SystemProfilePath, writable: true))
                {
                    if (sysProfile != null)
                    {
                        sysProfile.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        sysProfile.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    }
                }

                // 2. Games Task Profile (GPU & Scheduler High Priority)
                using (var gamesTask = Registry.LocalMachine.OpenSubKey(GamesTaskPath, writable: true))
                {
                    if (gamesTask != null)
                    {
                        gamesTask.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                        gamesTask.SetValue("Priority", 6, RegistryValueKind.DWord);
                        gamesTask.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                        gamesTask.SetValue("SFIO Priority", "High", RegistryValueKind.String);
                    }
                }

                // 3. TCP NoDelay & Immediate ACK for all active network interfaces
                using (var interfacesKey = Registry.LocalMachine.OpenSubKey(TcpInterfacesPath, writable: true))
                {
                    if (interfacesKey != null)
                    {
                        foreach (string subkeyName in interfacesKey.GetSubKeyNames())
                        {
                            try
                            {
                                using var ifaceKey = interfacesKey.OpenSubKey(subkeyName, writable: true);
                                if (ifaceKey != null)
                                {
                                    ifaceKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                    ifaceKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                }
                            }
                            catch { }
                        }
                    }
                }

                AppConfig.Set("gaming_network_opt", 1);
                Logger.WriteLine("[NetworkOptimizer] Applied Low-Latency TCP/UDP, NoDelay, and QoS gaming optimizations.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[NetworkOptimizer] Failed to apply network optimizations: {ex.Message}");
                return false;
            }
        }

        public static bool RestoreDefaultNetworkOptimizations()
        {
            try
            {
                using (var sysProfile = Registry.LocalMachine.OpenSubKey(SystemProfilePath, writable: true))
                {
                    if (sysProfile != null)
                    {
                        sysProfile.SetValue("NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                        sysProfile.SetValue("SystemResponsiveness", 20, RegistryValueKind.DWord);
                    }
                }

                using (var interfacesKey = Registry.LocalMachine.OpenSubKey(TcpInterfacesPath, writable: true))
                {
                    if (interfacesKey != null)
                    {
                        foreach (string subkeyName in interfacesKey.GetSubKeyNames())
                        {
                            try
                            {
                                using var ifaceKey = interfacesKey.OpenSubKey(subkeyName, writable: true);
                                if (ifaceKey != null)
                                {
                                    ifaceKey.DeleteValue("TcpAckFrequency", false);
                                    ifaceKey.DeleteValue("TCPNoDelay", false);
                                }
                            }
                            catch { }
                        }
                    }
                }

                AppConfig.Set("gaming_network_opt", 0);
                Logger.WriteLine("[NetworkOptimizer] Restored standard Windows network settings.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[NetworkOptimizer] Failed to restore network settings: {ex.Message}");
                return false;
            }
        }
    }
}
