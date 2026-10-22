using System.Drawing;
using System.Drawing.Imaging;
using GHelper.Helpers;

namespace GHelper.RGBStudio
{
    public enum ReactiveMode
    {
        Off,
        AudioSpectrum,
        ScreenAmbilight,
        ThermalHealth,
        BatteryStatus
    }

    public class RGBReactiveEngine
    {
        private static readonly Lazy<RGBReactiveEngine> _instance = new(() => new RGBReactiveEngine());
        public static RGBReactiveEngine Instance => _instance.Value;

        private ReactiveMode _currentMode = ReactiveMode.Off;
        private System.Windows.Forms.Timer? _reactiveTimer;
        private volatile float _currentAudioLevel = 0.0f;
        private Color _currentAmbilightColor = Color.Cyan;

        public ReactiveMode CurrentMode => _currentMode;
        public float CurrentAudioLevel => _currentAudioLevel;
        public Color CurrentAmbilightColor => _currentAmbilightColor;

        public event EventHandler? ReactiveFrameTicked;

        public RGBReactiveEngine()
        {
            AudioVisualizer.Shared.Subscribe(OnAudioDataReceived);
        }

        private void OnAudioDataReceived(double[] magnitudes)
        {
            if (_currentMode != ReactiveMode.AudioSpectrum || magnitudes == null || magnitudes.Length == 0)
                return;

            try
            {
                // Inspect bass & mid-range frequencies (first 25% of FFT bins)
                int sampleCount = Math.Min(64, magnitudes.Length);
                double sum = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    sum += magnitudes[i];
                }

                double avg = sum / sampleCount;
                // Normalize and smooth audio level between 0.0 and 1.0
                float normalized = (float)Math.Clamp(avg / 40.0, 0.0, 1.0);
                _currentAudioLevel = (_currentAudioLevel * 0.4f) + (normalized * 0.6f);
            }
            catch { }
        }

        public void SetMode(ReactiveMode mode)
        {
            _currentMode = mode;
            AppConfig.Set("rgb_reactive_mode", (int)mode);
            Logger.WriteLine($"[RGBReactiveEngine] Reactive lighting mode set to: {mode}");

            if (mode == ReactiveMode.Off)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
        }

        private void StartTimer()
        {
            if (_reactiveTimer != null) return;
            _reactiveTimer = new System.Windows.Forms.Timer();
            _reactiveTimer.Interval = 40; // ~25 FPS
            _reactiveTimer.Tick += ReactiveTimer_Tick;
            _reactiveTimer.Start();
        }

        private void StopTimer()
        {
            _reactiveTimer?.Stop();
            _reactiveTimer?.Dispose();
            _reactiveTimer = null;
        }

        private void ReactiveTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                switch (_currentMode)
                {
                    case ReactiveMode.ScreenAmbilight:
                        SampleScreenAmbilight();
                        ApplyAmbilightToHardware();
                        break;

                    case ReactiveMode.AudioSpectrum:
                        ApplyAudioLevelToHardware();
                        break;

                    case ReactiveMode.ThermalHealth:
                        ApplyThermalToHardware();
                        break;
                }

                ReactiveFrameTicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[RGBReactiveEngine] Frame error: {ex.Message}");
            }
        }

        private void SampleScreenAmbilight()
        {
            try
            {
                var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                // Sample 32x18 thumbnail for ultra-low CPU overhead
                using var bmp = new Bitmap(32, 18, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(32, 18), CopyPixelOperation.SourceCopy);
                }

                long rSum = 0, gSum = 0, bSum = 0;
                int pixels = bmp.Width * bmp.Height;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var px = bmp.GetPixel(x, y);
                        rSum += px.R;
                        gSum += px.G;
                        bSum += px.B;
                    }
                }

                int avgR = Math.Clamp((int)(rSum / pixels), 0, 255);
                int avgG = Math.Clamp((int)(gSum / pixels), 0, 255);
                int avgB = Math.Clamp((int)(bSum / pixels), 0, 255);

                // Boost saturation slightly for vivid RGB lighting
                Color raw = Color.FromArgb(avgR, avgG, avgB);
                float hue = raw.GetHue();
                float sat = Math.Min(1.0f, raw.GetSaturation() * 1.3f);
                float bri = Math.Clamp(raw.GetBrightness() * 1.2f, 0.2f, 1.0f);

                _currentAmbilightColor = RGBColorUtilities.ColorFromHSV(hue, sat, bri);
            }
            catch { }
        }

        private void ApplyAmbilightToHardware()
        {
            foreach (var dev in RGBDeviceManager.Instance.AllDevices)
            {
                if (dev.IsConnected || dev.IsSimulated)
                {
                    dev.ApplyStaticColor(_currentAmbilightColor);
                }
            }
        }

        private void ApplyAudioLevelToHardware()
        {
            var config = new RGBEffectConfig
            {
                EffectId = "AudioReactive",
                PrimaryColor = Color.Cyan,
                SecondaryColor = Color.HotPink,
                Brightness = 100
            };

            foreach (var dev in RGBDeviceManager.Instance.AllDevices)
            {
                if (dev.IsConnected || dev.IsSimulated)
                {
                    dev.ApplyEffect(config);
                }
            }
        }

        private void ApplyThermalToHardware()
        {
            float cpu = HardwareControl.cpuTemp ?? 45.0f;
            float metric = Math.Clamp((cpu - 40.0f) / 50.0f, 0.0f, 1.0f);

            var config = new RGBEffectConfig
            {
                EffectId = "CpuTempRGB",
                Brightness = 100
            };

            foreach (var dev in RGBDeviceManager.Instance.AllDevices)
            {
                if (dev.IsConnected || dev.IsSimulated)
                {
                    dev.ApplyEffect(config);
                }
            }
        }
    }
}
