using System.Drawing;
using GHelper.Helpers;

namespace GHelper.RGBStudio
{
    public class RGBDeviceManager
    {
        private static readonly Lazy<RGBDeviceManager> _instance = new(() => new RGBDeviceManager());
        public static RGBDeviceManager Instance => _instance.Value;

        public AsusLaptopLightingProvider AsusProvider { get; } = new();
        public KreoLightingProvider KreoKeyboardProvider { get; } = new();
        public KreoMouseProvider KreoMouseProvider { get; } = new();

        public List<IRGBDeviceProvider> AllDevices => [AsusProvider, KreoKeyboardProvider, KreoMouseProvider];

        public bool SyncAllDevices { get; set; } = false;
        public RGBEffectConfig CurrentEffectConfig { get; set; } = new();

        private System.Windows.Forms.Timer? _previewTimer;
        private double _animationTime = 0.0;
        public event EventHandler? FrameUpdated;

        public RGBDeviceManager()
        {
            _previewTimer = new System.Windows.Forms.Timer();
            _previewTimer.Interval = 33; // ~30 FPS for smooth previews
            _previewTimer.Tick += PreviewTimer_Tick;
            _previewTimer.Start();
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            _animationTime += 0.033;
            FrameUpdated?.Invoke(this, EventArgs.Empty);
        }

        public double CurrentAnimationTime => _animationTime;

        public void ApplyConfigToAll(RGBEffectConfig config)
        {
            CurrentEffectConfig = config.Clone();
            foreach (var dev in AllDevices)
            {
                try
                {
                    dev.ApplyEffect(CurrentEffectConfig);
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"[RGBManager] Apply error for {dev.DeviceName}: {ex.Message}");
                }
            }
        }

        public void ApplyTheme(RGBTheme theme)
        {
            var config = theme.ToConfig();
            ApplyConfigToAll(config);
        }

        public void RefreshConnectedHardware()
        {
            KreoKeyboardProvider.ScanForDevice();
            KreoMouseProvider.ScanForDevice();
        }
    }
}
