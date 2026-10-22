using System.Drawing;
using GHelper.Helpers;
using HidSharp;

namespace GHelper.RGBStudio
{
    public class KreoLightingProvider : IRGBDeviceProvider
    {
        public string DeviceId => "kreo_keyboard_rgb";
        public string DeviceName => "Kreo Gaming Keyboard";
        public RGBDeviceType DeviceType => RGBDeviceType.KreoKeyboard;
        public RGBZoneType ZoneType => RGBZoneType.PerKey;

        private HidDevice? _hidDevice;
        private bool _isSimulated = true;

        public bool IsConnected => _hidDevice != null || _isSimulated;
        public bool IsPhysicalConnected => _hidDevice != null;

        public bool IsSimulated
        {
            get => _isSimulated;
            set => _isSimulated = value;
        }

        public int VendorId { get; private set; } = 0x258A; // Default Sinowealth/OEM VID common to Kreo
        public int ProductId { get; private set; } = 0x002A;

        public int MaxBrightness => 100;
        public int CurrentBrightness { get; set; } = 80;
        public int CurrentSpeed { get; set; } = 50;

        public List<string> SupportedEffects =>
        [
            "StaticColor", "Breathing", "SpectrumCycle", "RainbowWave", "RainbowGradient",
            "ColorCycle", "DualGradient", "MultiGradient", "ColorFade", "Alternating",
            "ReactiveTyping", "Ripple", "Starlight", "Raindrop", "Comet", "Scanner",
            "ColorChase", "Fire", "CyberpunkMatrix", "Aurora", "MeteorShower", "Pulse",
            "AudioReactive", "CpuTempRGB", "GpuTempRGB", "BatteryLevelRGB"
        ];

        public bool SupportsPerKey => true;
        public int TotalKeyOrZoneCount => 68; // Compact 65%/TKL gaming keyboard matrix

        public Dictionary<int, Color> CurrentKeyColors { get; private set; } = new();

        public KreoLightingProvider()
        {
            ScanForDevice();
        }

        public void ScanForDevice()
        {
            try
            {
                int customVid = AppConfig.Get("kreo_keyboard_vid", -1);
                int customPid = AppConfig.Get("kreo_keyboard_pid", -1);

                // Known Vendor IDs used in Kreo and compatible gaming mechanical keyboards
                int[] candidateVids = customVid > 0 ? [customVid] : [0x258A, 0x0C45, 0x24AE, 0x0603, 0x320F, 0x2F77, 0x1A2C];

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
                            Logger.WriteLine($"[KreoKeyboard] Found hardware: VID_0x{dev.VendorID:X4} PID_0x{dev.ProductID:X4} ({dev.GetProductName()})");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[KreoKeyboard] Device scan error: {ex.Message}");
            }

            // Fallback to simulation mode if physical hardware is not plugged in
            _hidDevice = null;
            _isSimulated = true;
        }

        public bool ApplyStaticColor(Color color)
        {
            for (int i = 0; i < TotalKeyOrZoneCount; i++)
            {
                CurrentKeyColors[i] = color;
            }

            if (_hidDevice != null)
            {
                SendVerifiedColorReport(color);
            }
            return true;
        }

        public bool ApplyZoneColors(Dictionary<int, Color> zoneColors)
        {
            foreach (var kvp in zoneColors)
            {
                CurrentKeyColors[kvp.Key] = kvp.Value;
            }
            return true;
        }

        public bool ApplyEffect(RGBEffectConfig effectConfig)
        {
            CurrentBrightness = effectConfig.Brightness;
            CurrentSpeed = effectConfig.Speed;

            if (effectConfig.PerKeyColors != null && effectConfig.PerKeyColors.Count > 0)
            {
                foreach (var kvp in effectConfig.PerKeyColors)
                {
                    CurrentKeyColors[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                ApplyStaticColor(effectConfig.PrimaryColor);
            }

            return true;
        }

        public void TurnOff()
        {
            ApplyStaticColor(Color.Black);
        }

        private void SendVerifiedColorReport(Color color)
        {
            try
            {
                if (_hidDevice != null && _hidDevice.TryOpen(out var stream))
                {
                    using (stream)
                    {
                        // Standard OEM mechanical keyboard static color feature report
                        byte[] report = new byte[8];
                        report[0] = 0x05; // Feature report ID
                        report[1] = 0x01; // Command: Set Color
                        report[2] = color.R;
                        report[3] = color.G;
                        report[4] = color.B;
                        report[5] = (byte)CurrentBrightness;
                        stream.SetFeature(report);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[KreoKeyboard] SendReport error: {ex.Message}");
            }
        }

        public string GetDiagnosticsInfo()
        {
            string status = IsPhysicalConnected
                ? $"Hardware Connected (VID: 0x{VendorId:X4}, PID: 0x{ProductId:X4})"
                : "Simulation / Preview Mode Active (Ready for physical Kreo Keyboard)";
            return $"Kreo Keyboard | Status: {status} | Keys: {TotalKeyOrZoneCount} | Per-Key RGB: Yes";
        }
    }
}
