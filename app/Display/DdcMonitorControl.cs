using System.Diagnostics;
using System.Runtime.InteropServices;
using GHelper.Helpers;

namespace GHelper.Display
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public class ConnectedMonitorInfo
    {
        public string Id { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string FriendlyName { get; set; } = "Generic Monitor";
        public bool IsInternal { get; set; }
        public bool IsPrimary { get; set; }
        public Rectangle Bounds { get; set; }
        public int RefreshRate { get; set; }
        public IntPtr HMonitor { get; set; }
        public IntPtr HPhysicalMonitor { get; set; }
        public bool SupportsDdc { get; set; }

        public int MinBrightness { get; set; } = 0;
        public int MaxBrightness { get; set; } = 100;
        public int CurrentBrightness { get; set; } = 100;

        public int MinContrast { get; set; } = 0;
        public int MaxContrast { get; set; } = 100;
        public int CurrentContrast { get; set; } = 50;

        public bool SupportsVolume { get; set; }
        public int CurrentVolume { get; set; } = 50;

        public int CurrentInputSource { get; set; } = 0;
    }

    public static class DdcNative
    {
        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorContrast(IntPtr hMonitor, out uint pdwMinimumContrast, out uint pdwCurrentContrast, out uint pdwMaximumContrast);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorContrast(IntPtr hMonitor, uint dwNewContrast);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetVCPFeatureAndVCPFeatureCodes(IntPtr hMonitor, byte bVCPCode, out uint pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        // VCP Feature Codes
        public const byte VCP_BRIGHTNESS = 0x10;
        public const byte VCP_CONTRAST = 0x12;
        public const byte VCP_VOLUME = 0x62;
        public const byte VCP_INPUT_SOURCE = 0x60;
        public const byte VCP_POWER_MODE = 0xD6;

        // Power mode values
        public const uint POWER_ON = 1;
        public const uint POWER_STANDBY = 4;
        public const uint POWER_OFF = 5;
    }

    public static class DdcMonitorManager
    {
        private static readonly List<PHYSICAL_MONITOR> _allocatedPhysicalMonitors = [];

        public static List<ConnectedMonitorInfo> EnumerateMonitors()
        {
            var list = new List<ConnectedMonitorInfo>();
            CleanupPhysicalMonitors();

            try
            {
                DdcNative.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdcMon, ref RECT rc, IntPtr data) =>
                {
                    var mi = new MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                    if (!DdcNative.GetMonitorInfo(hMon, ref mi)) return true;

                    bool isPrimary = (mi.dwFlags & 1) != 0;
                    Rectangle bounds = new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
                    string devName = mi.szDevice;

                    // Check if internal screen
                    string laptopScreen = ScreenNative.FindLaptopScreen(true);
                    bool isInternal = string.Equals(devName, laptopScreen, StringComparison.OrdinalIgnoreCase);

                    // Try getting physical monitors via DXVA2
                    if (DdcNative.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out uint count) && count > 0)
                    {
                        var phys = new PHYSICAL_MONITOR[count];
                        if (DdcNative.GetPhysicalMonitorsFromHMONITOR(hMon, count, phys))
                        {
                            foreach (var p in phys)
                            {
                                _allocatedPhysicalMonitors.Add(p);

                                string friendly = p.szPhysicalMonitorDescription;
                                if (string.IsNullOrWhiteSpace(friendly) || friendly == "Generic PnP Monitor")
                                {
                                    friendly = isInternal ? "Built-in ROG Nebula Display" : $"External Monitor ({bounds.Width}x{bounds.Height})";
                                }

                                var monInfo = new ConnectedMonitorInfo
                                {
                                    Id = $"{devName}_{p.hPhysicalMonitor}",
                                    DeviceName = devName,
                                    FriendlyName = friendly,
                                    IsInternal = isInternal,
                                    IsPrimary = isPrimary,
                                    Bounds = bounds,
                                    HMonitor = hMon,
                                    HPhysicalMonitor = p.hPhysicalMonitor,
                                    RefreshRate = ScreenNative.GetRefreshRate(devName)
                                };

                                // Probe brightness
                                if (DdcNative.GetMonitorBrightness(p.hPhysicalMonitor, out uint minB, out uint curB, out uint maxB))
                                {
                                    monInfo.SupportsDdc = true;
                                    monInfo.MinBrightness = (int)minB;
                                    monInfo.CurrentBrightness = (int)curB;
                                    monInfo.MaxBrightness = (int)maxB;
                                }
                                else if (isInternal)
                                {
                                    monInfo.SupportsDdc = true;
                                    monInfo.CurrentBrightness = ScreenBrightness.Get();
                                }
                                else
                                {
                                    // Try VCP code directly
                                    if (DdcNative.GetVCPFeatureAndVCPFeatureCodes(p.hPhysicalMonitor, DdcNative.VCP_BRIGHTNESS, out _, out uint curV, out uint maxV))
                                    {
                                        monInfo.SupportsDdc = true;
                                        monInfo.CurrentBrightness = (int)curV;
                                        monInfo.MaxBrightness = (int)maxV;
                                    }
                                    else
                                    {
                                        monInfo.CurrentBrightness = AppConfig.Get($"monitor_b_{devName}", 100);
                                    }
                                }

                                // Probe contrast
                                if (DdcNative.GetMonitorContrast(p.hPhysicalMonitor, out uint minC, out uint curC, out uint maxC))
                                {
                                    monInfo.MinContrast = (int)minC;
                                    monInfo.CurrentContrast = (int)curC;
                                    monInfo.MaxContrast = (int)maxC;
                                }
                                else
                                {
                                    monInfo.CurrentContrast = AppConfig.Get($"monitor_c_{devName}", 50);
                                }

                                // Probe volume
                                if (DdcNative.GetVCPFeatureAndVCPFeatureCodes(p.hPhysicalMonitor, DdcNative.VCP_VOLUME, out _, out uint curVol, out _))
                                {
                                    monInfo.SupportsVolume = true;
                                    monInfo.CurrentVolume = (int)curVol;
                                }

                                list.Add(monInfo);
                            }
                            return true;
                        }
                    }

                    // Fallback for internal screen or screens without DXVA physical handles
                    string nameFallback = isInternal ? "Built-in ROG Nebula Display" : $"Connected Display ({bounds.Width}x{bounds.Height})";
                    list.Add(new ConnectedMonitorInfo
                    {
                        Id = devName,
                        DeviceName = devName,
                        FriendlyName = nameFallback,
                        IsInternal = isInternal,
                        IsPrimary = isPrimary,
                        Bounds = bounds,
                        HMonitor = hMon,
                        RefreshRate = ScreenNative.GetRefreshRate(devName),
                        CurrentBrightness = isInternal ? ScreenBrightness.Get() : AppConfig.Get($"monitor_b_{devName}", 100),
                        CurrentContrast = 50,
                        SupportsDdc = isInternal
                    });

                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[DDC] Monitor enumeration error: {ex.Message}");
            }

            return list;
        }

        public static bool SetBrightness(ConnectedMonitorInfo mon, int brightness)
        {
            brightness = Math.Clamp(brightness, 0, 100);
            mon.CurrentBrightness = brightness;
            AppConfig.Set($"monitor_b_{mon.DeviceName}", brightness);

            if (mon.IsInternal)
            {
                ScreenBrightness.Set(brightness);
                return true;
            }

            if (mon.HPhysicalMonitor != IntPtr.Zero)
            {
                try
                {
                    if (DdcNative.SetMonitorBrightness(mon.HPhysicalMonitor, (uint)brightness))
                        return true;

                    if (DdcNative.SetVCPFeature(mon.HPhysicalMonitor, DdcNative.VCP_BRIGHTNESS, (uint)brightness))
                        return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"[DDC] SetBrightness error on {mon.FriendlyName}: {ex.Message}");
                }
            }

            return false;
        }

        public static bool SetContrast(ConnectedMonitorInfo mon, int contrast)
        {
            contrast = Math.Clamp(contrast, 0, 100);
            mon.CurrentContrast = contrast;
            AppConfig.Set($"monitor_c_{mon.DeviceName}", contrast);

            if (mon.HPhysicalMonitor != IntPtr.Zero)
            {
                try
                {
                    if (DdcNative.SetMonitorContrast(mon.HPhysicalMonitor, (uint)contrast))
                        return true;

                    if (DdcNative.SetVCPFeature(mon.HPhysicalMonitor, DdcNative.VCP_CONTRAST, (uint)contrast))
                        return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"[DDC] SetContrast error on {mon.FriendlyName}: {ex.Message}");
                }
            }

            return false;
        }

        public static bool SetVolume(ConnectedMonitorInfo mon, int volume)
        {
            volume = Math.Clamp(volume, 0, 100);
            mon.CurrentVolume = volume;

            if (mon.HPhysicalMonitor != IntPtr.Zero)
            {
                try
                {
                    return DdcNative.SetVCPFeature(mon.HPhysicalMonitor, DdcNative.VCP_VOLUME, (uint)volume);
                }
                catch { }
            }
            return false;
        }

        public static bool SetInputSource(ConnectedMonitorInfo mon, int inputCode)
        {
            mon.CurrentInputSource = inputCode;
            if (mon.HPhysicalMonitor != IntPtr.Zero)
            {
                try
                {
                    return DdcNative.SetVCPFeature(mon.HPhysicalMonitor, DdcNative.VCP_INPUT_SOURCE, (uint)inputCode);
                }
                catch { }
            }
            return false;
        }

        public static bool SetPowerMode(ConnectedMonitorInfo mon, uint powerMode)
        {
            if (mon.HPhysicalMonitor != IntPtr.Zero)
            {
                try
                {
                    return DdcNative.SetVCPFeature(mon.HPhysicalMonitor, DdcNative.VCP_POWER_MODE, powerMode);
                }
                catch { }
            }
            return false;
        }

        public static void SetAllBrightness(int brightness)
        {
            var monitors = EnumerateMonitors();
            foreach (var m in monitors)
            {
                SetBrightness(m, brightness);
            }
        }

        public static void AdjustAllBrightness(int delta)
        {
            var monitors = EnumerateMonitors();
            foreach (var m in monitors)
            {
                int newB = Math.Clamp(m.CurrentBrightness + delta, 0, 100);
                SetBrightness(m, newB);
            }
        }

        public static void CleanupPhysicalMonitors()
        {
            if (_allocatedPhysicalMonitors.Count > 0)
            {
                try
                {
                    DdcNative.DestroyPhysicalMonitors((uint)_allocatedPhysicalMonitors.Count, [.. _allocatedPhysicalMonitors]);
                }
                catch { }
                _allocatedPhysicalMonitors.Clear();
            }
        }
    }
}
