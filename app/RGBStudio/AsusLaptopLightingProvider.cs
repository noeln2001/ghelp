using System.Drawing;
using GHelper.Helpers;
using GHelper.USB;

namespace GHelper.RGBStudio
{
    public class AsusLaptopLightingProvider : IRGBDeviceProvider
    {
        public string DeviceId => "asus_strix_g16_keyboard";
        public string DeviceName => "ASUS ROG Strix G16 Keyboard";
        public RGBDeviceType DeviceType => RGBDeviceType.AsusLaptopKeyboard;
        public RGBZoneType ZoneType => Aura.BacklightType == AuraBacklightType.PerKey ? RGBZoneType.PerKey : RGBZoneType.MultiZone4;

        public bool IsConnected => Aura.IsBacklightDetected;
        public bool IsSimulated { get; set; } = false;
        public int VendorId => 0x0B05; // ASUS
        public int ProductId => 0x19B6; // ROG Keyboard

        public int MaxBrightness => 3;
        public int CurrentBrightness
        {
            get => Input.InputDispatcher.GetBacklight();
            set => Aura.ApplyBrightness(value, "RGBStudio");
        }

        public int CurrentSpeed { get; set; } = 1;

        public List<string> SupportedEffects =>
        [
            "StaticColor", "Breathing", "SpectrumCycle", "RainbowWave",
            "Comet", "Starlight", "Raindrop", "AudioReactive", "CpuTempRGB", "GpuTempRGB"
        ];

        public bool SupportsPerKey => Aura.BacklightType == AuraBacklightType.PerKey;
        public int TotalKeyOrZoneCount => SupportsPerKey ? 104 : 4;

        public bool ApplyStaticColor(Color color)
        {
            try
            {
                Aura.Color1 = color;
                Aura.Mode = AuraMode.AuraStatic;
                Aura.ApplyAura();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AsusLighting] ApplyStaticColor error: {ex.Message}");
                return false;
            }
        }

        public bool ApplyZoneColors(Dictionary<int, Color> zoneColors)
        {
            try
            {
                if (zoneColors.TryGetValue(0, out var c1)) Aura.Color1 = c1;
                if (zoneColors.TryGetValue(1, out var c2)) Aura.Color2 = c2;
                Aura.ApplyAura();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AsusLighting] ApplyZoneColors error: {ex.Message}");
                return false;
            }
        }

        public bool ApplyEffect(RGBEffectConfig effectConfig)
        {
            try
            {
                Aura.Color1 = effectConfig.PrimaryColor;
                Aura.Color2 = effectConfig.SecondaryColor;

                switch (effectConfig.EffectId)
                {
                    case "StaticColor":
                        Aura.Mode = AuraMode.AuraStatic;
                        break;
                    case "Breathing":
                        Aura.Mode = AuraMode.AuraBreathe;
                        break;
                    case "SpectrumCycle":
                        Aura.Mode = AuraMode.AuraColorCycle;
                        break;
                    case "RainbowWave":
                    case "RainbowGradient":
                        Aura.Mode = AuraMode.AuraRainbow;
                        break;
                    case "Comet":
                        Aura.Mode = AuraMode.Comet;
                        break;
                    case "Starlight":
                        Aura.Mode = AuraMode.Star;
                        break;
                    case "Raindrop":
                        Aura.Mode = AuraMode.Rain;
                        break;
                    case "AudioReactive":
                        Aura.Mode = AuraMode.AUDIO;
                        break;
                    case "CpuTempRGB":
                    case "GpuTempRGB":
                        Aura.Mode = AuraMode.HEATMAP;
                        break;
                    default:
                        Aura.Mode = AuraMode.AuraStatic;
                        break;
                }

                Aura.ApplyAura();
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AsusLighting] ApplyEffect error: {ex.Message}");
                return false;
            }
        }

        public void TurnOff()
        {
            Aura.ApplyBrightness(0, "RGBStudio_Off");
        }

        public string GetDiagnosticsInfo()
        {
            return $"ASUS ROG Strix G16 Lighting | Type: {Aura.BacklightType} | Connected: {IsConnected} | Logo: {Aura.HasLogo} | Lightbar: {Aura.HasLightbar}";
        }
    }
}
