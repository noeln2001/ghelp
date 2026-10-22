using System.Drawing;
using GHelper.Helpers;
using HidSharp;

namespace GHelper.RGBStudio
{
    public class KreoMouseProvider : IRGBDeviceProvider
    {
        public string DeviceId => "kreo_mouse_rgb";
        public string DeviceName => "Kreo Gaming Mouse";
        public RGBDeviceType DeviceType => RGBDeviceType.KreoMouse;
        public RGBZoneType ZoneType => RGBZoneType.Logo;

        private HidDevice? _hidDevice;
        private bool _isSimulated = true;

        public bool IsConnected => _hidDevice != null || _isSimulated;
        public bool IsPhysicalConnected => _hidDevice != null;

        public bool IsSimulated
        {
            get => _isSimulated;
            set => _isSimulated = value;
        }

        public int VendorId { get; private set; } = 0x258A;
        public int ProductId { get; private set; } = 0x2011;

        public int MaxBrightness => 100;
        public int CurrentBrightness { get; set; } = 90;
        public int CurrentSpeed { get; set; } = 50;

        public int CurrentDpi { get; set; } = 1600;
        public int CurrentPollingRate { get; set; } = 1000;

        public static readonly int[] AvailableDpiPresets = [400, 800, 1200, 1600, 2400, 3200, 6400, 12000, 26000];
        public static readonly int[] AvailablePollingRates = [125, 250, 500, 1000, 2000, 4000];

        public List<string> SupportedEffects =>
        [
            "StaticColor", "Breathing", "SpectrumCycle", "RainbowWave",
            "ColorCycle", "DualGradient", "Pulse", "NeonGlow",
            "CpuTempRGB", "GpuTempRGB", "BatteryLevelRGB"
        ];

        public bool SupportsPerKey => false;
        public int TotalKeyOrZoneCount => 4; // 0: Logo, 1: Scroll Wheel, 2: Side Strip, 3: Underglow

        public Dictionary<int, Color> ZoneColors { get; private set; } = new()
        {
            { 0, Color.Cyan },
            { 1, Color.Magenta },
            { 2, Color.DeepSkyBlue },
            { 3, Color.HotPink }
        };

        public KreoMouseProvider()
        {
            ScanForDevice();
        }

        public void ScanForDevice()
        {
            try
            {
                int customVid = AppConfig.Get("kreo_mouse_vid", -1);
                int customPid = AppConfig.Get("kreo_mouse_pid", -1);

                int[] candidateVids = customVid > 0 ? [customVid] : [0x258A, 0x0C45, 0x24AE, 0x1B17, 0x320F, 0x2F77];

                foreach (var dev in DeviceList.Local.GetHidDevices())
                {
                    if (candidateVids.Contains(dev.VendorID))
                    {
                        if (customPid <= 0 || dev.ProductID == customPid)
                        {
                            _hidDevice = dev;
                            VendorId = dev.VendorID;
                            ProductId = dev.ProductID;
                            _isSimulated = false;
                            Logger.WriteLine($"[KreoMouse] Found hardware: VID_0x{dev.VendorID:X4} PID_0x{dev.ProductID:X4} ({dev.GetProductName()})");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[KreoMouse] Device scan error: {ex.Message}");
            }

            _hidDevice = null;
            _isSimulated = true;
        }

        public bool ApplyStaticColor(Color color)
        {
            for (int i = 0; i < TotalKeyOrZoneCount; i++)
            {
                ZoneColors[i] = color;
            }
            return true;
        }

        public bool ApplyZoneColors(Dictionary<int, Color> zoneColors)
        {
            foreach (var kvp in zoneColors)
            {
                ZoneColors[kvp.Key] = kvp.Value;
            }
            return true;
        }

        public bool ApplyEffect(RGBEffectConfig effectConfig)
        {
            CurrentBrightness = effectConfig.Brightness;
            CurrentSpeed = effectConfig.Speed;
            ApplyStaticColor(effectConfig.PrimaryColor);
            return true;
        }

        public void TurnOff()
        {
            ApplyStaticColor(Color.Black);
        }

        public string GetDiagnosticsInfo()
        {
            string status = IsPhysicalConnected
                ? $"Hardware Connected (VID: 0x{VendorId:X4}, PID: 0x{ProductId:X4})"
                : "Simulation / Preview Mode Active (Ready for physical Kreo Mouse)";
            return $"Kreo Mouse | Status: {status} | DPI: {CurrentDpi} | Polling: {CurrentPollingRate}Hz | Zones: Logo, Scroll, SideStrip, Underglow";
        }
    }
}
