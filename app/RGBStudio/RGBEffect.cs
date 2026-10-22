using System.Drawing;

namespace GHelper.RGBStudio
{
    public enum EffectCategory
    {
        Basic,
        AdvancedAnimated,
        IntelligentReactive
    }

    public enum EffectDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
        CenterOutwards,
        InwardsToCenter
    }

    public class RGBEffectDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public EffectCategory Category { get; set; }
        public string Description { get; set; } = "";
    }

    public class RGBEffectConfig
    {
        public string EffectId { get; set; } = "RainbowWave";
        public Color PrimaryColor { get; set; } = Color.Cyan;
        public Color SecondaryColor { get; set; } = Color.Magenta;
        public Color AccentColor { get; set; } = Color.Yellow;
        public Color BackgroundColor { get; set; } = Color.Black;
        public int Brightness { get; set; } = 100;
        public int Speed { get; set; } = 50;
        public EffectDirection Direction { get; set; } = EffectDirection.LeftToRight;
        public int Intensity { get; set; } = 100;
        public Dictionary<int, Color> PerKeyColors { get; set; } = new();

        public RGBEffectConfig Clone()
        {
            return new RGBEffectConfig
            {
                EffectId = EffectId,
                PrimaryColor = PrimaryColor,
                SecondaryColor = SecondaryColor,
                AccentColor = AccentColor,
                BackgroundColor = BackgroundColor,
                Brightness = Brightness,
                Speed = Speed,
                Direction = Direction,
                Intensity = Intensity,
                PerKeyColors = new Dictionary<int, Color>(PerKeyColors)
            };
        }
    }

    public static class RGBEffectsLibrary
    {
        public static readonly List<RGBEffectDefinition> AllEffects =
        [
            // 1-10: Basic Effects
            new() { Id = "StaticColor", Name = "Static Color", Category = EffectCategory.Basic, Description = "Constant solid color lighting" },
            new() { Id = "Breathing", Name = "Breathing", Category = EffectCategory.Basic, Description = "Smooth fading in and out between primary and background" },
            new() { Id = "SpectrumCycle", Name = "Spectrum Cycle", Category = EffectCategory.Basic, Description = "Continuous rotation across the full color spectrum" },
            new() { Id = "RainbowWave", Name = "Rainbow Wave", Category = EffectCategory.Basic, Description = "Dynamic rolling rainbow wave" },
            new() { Id = "RainbowGradient", Name = "Rainbow Gradient", Category = EffectCategory.Basic, Description = "Smooth full-palette spatial gradient" },
            new() { Id = "ColorCycle", Name = "Color Cycle", Category = EffectCategory.Basic, Description = "Cycles through selected custom color palette" },
            new() { Id = "DualGradient", Name = "Dual-Color Gradient", Category = EffectCategory.Basic, Description = "Linear blend between primary and secondary colors" },
            new() { Id = "MultiGradient", Name = "Multi-Color Gradient", Category = EffectCategory.Basic, Description = "Three-stop gradient across primary, secondary and accent" },
            new() { Id = "ColorFade", Name = "Color Fade", Category = EffectCategory.Basic, Description = "Gentle transition between primary and secondary" },
            new() { Id = "Alternating", Name = "Alternating Colors", Category = EffectCategory.Basic, Description = "Interleaved alternating checkerboard colors" },

            // 11-27: Advanced Animated Effects
            new() { Id = "ReactiveTyping", Name = "Reactive Typing", Category = EffectCategory.AdvancedAnimated, Description = "Keys illuminate on keypress and fade away" },
            new() { Id = "Ripple", Name = "Ripple", Category = EffectCategory.AdvancedAnimated, Description = "Expanding circular waves emanating outwards" },
            new() { Id = "Starlight", Name = "Starlight", Category = EffectCategory.AdvancedAnimated, Description = "Subtle twinkling stars across the surface" },
            new() { Id = "Raindrop", Name = "Raindrop", Category = EffectCategory.AdvancedAnimated, Description = "Smooth simulated raindrops trickling downwards" },
            new() { Id = "Comet", Name = "Comet", Category = EffectCategory.AdvancedAnimated, Description = "Sweeping light streak with fading tail" },
            new() { Id = "Scanner", Name = "Scanner", Category = EffectCategory.AdvancedAnimated, Description = "Cylon / Knight Rider style back-and-forth beam" },
            new() { Id = "ColorChase", Name = "Color Chase", Category = EffectCategory.AdvancedAnimated, Description = "Chasing light streaks across zones" },
            new() { Id = "Fire", Name = "Fire & Flame", Category = EffectCategory.AdvancedAnimated, Description = "Warm flickering embers and rising fire flames" },
            new() { Id = "CyberpunkMatrix", Name = "Cyberpunk Matrix", Category = EffectCategory.AdvancedAnimated, Description = "Neon green and cyan digital rain code cascades" },
            new() { Id = "Aurora", Name = "Aurora Borealis", Category = EffectCategory.AdvancedAnimated, Description = "Ethereal undulating green, purple, and blue ribbons" },
            new() { Id = "MeteorShower", Name = "Meteor Shower", Category = EffectCategory.AdvancedAnimated, Description = "High-velocity streaks across keys" },
            new() { Id = "WavePropagation", Name = "Wave Propagation", Category = EffectCategory.AdvancedAnimated, Description = "Multi-frequency sinusoidal waves" },
            new() { Id = "Pulse", Name = "Pulse", Category = EffectCategory.AdvancedAnimated, Description = "Rhythmic heartbeat glow" },
            new() { Id = "NeonGlow", Name = "Neon Glow", Category = EffectCategory.AdvancedAnimated, Description = "Vibrant synthwave aesthetic glows" },
            new() { Id = "Sparkle", Name = "Sparkle", Category = EffectCategory.AdvancedAnimated, Description = "Bright random glimmering bursts" },
            new() { Id = "RandomBursts", Name = "Random Color Bursts", Category = EffectCategory.AdvancedAnimated, Description = "Randomized multi-color flare points" },
            new() { Id = "CustomSequence", Name = "Custom Sequence", Category = EffectCategory.AdvancedAnimated, Description = "User programmed keyframe sequence" },

            // 28-37: Intelligent Hardware Reactive Effects
            new() { Id = "AudioReactive", Name = "Audio-Reactive Spectrum", Category = EffectCategory.IntelligentReactive, Description = "Real-time equalizer lighting synced to system audio" },
            new() { Id = "CpuTempRGB", Name = "CPU Temperature RGB", Category = EffectCategory.IntelligentReactive, Description = "Green (cool) to Yellow (warm) to Red (hot) based on CPU" },
            new() { Id = "GpuTempRGB", Name = "GPU Temperature RGB", Category = EffectCategory.IntelligentReactive, Description = "Color shifts based on RTX 4060 GPU temperature" },
            new() { Id = "GpuUsageRGB", Name = "GPU Utilization RGB", Category = EffectCategory.IntelligentReactive, Description = "Lighting intensity scales with GPU rendering load" },
            new() { Id = "BatteryLevelRGB", Name = "Battery Level RGB", Category = EffectCategory.IntelligentReactive, Description = "RGB bar / zone representing charge %" },
            new() { Id = "ChargingStatusRGB", Name = "Charging Status RGB", Category = EffectCategory.IntelligentReactive, Description = "Pulsing green on AC charge, calm cyan on battery" },
            new() { Id = "FpsReactiveRGB", Name = "FPS-Based RGB", Category = EffectCategory.IntelligentReactive, Description = "Color changes according to live game frame rate" },
            new() { Id = "GameStateRGB", Name = "Game-State RGB", Category = EffectCategory.IntelligentReactive, Description = "Lighting responds to game events, alerts, and combat" },
            new() { Id = "GameSpecificRGB", Name = "Game-Specific Theme", Category = EffectCategory.IntelligentReactive, Description = "Custom hand-crafted palette for active title" },
            new() { Id = "CustomUserEffect", Name = "Custom User Effect", Category = EffectCategory.IntelligentReactive, Description = "Custom multi-layer scriptable RGB configuration" }
        ];

        /// <summary>
        /// Computes the color for a specific zone or key at time t (in seconds).
        /// </summary>
        public static Color ComputeColor(RGBEffectConfig config, int keyIndex, int totalKeys, double timeSeconds, double hardwareMetric = 0.5)
        {
            double speedFactor = Math.Max(0.1, config.Speed / 50.0);
            double progress = (timeSeconds * speedFactor) % 1.0;
            double positionFactor = totalKeys > 1 ? (double)keyIndex / (totalKeys - 1) : 0.0;

            Color baseColor;

            switch (config.EffectId)
            {
                case "StaticColor":
                    if (config.PerKeyColors != null && config.PerKeyColors.TryGetValue(keyIndex, out var customColor))
                        baseColor = customColor;
                    else
                        baseColor = config.PrimaryColor;
                    break;

                case "Breathing":
                    double breath = (Math.Sin(timeSeconds * speedFactor * Math.PI * 2) + 1.0) / 2.0;
                    baseColor = RGBColorUtilities.Interpolate(config.BackgroundColor, config.PrimaryColor, breath);
                    break;

                case "SpectrumCycle":
                    double hue = (timeSeconds * speedFactor * 60.0) % 360.0;
                    baseColor = RGBColorUtilities.ColorFromHSV(hue, 1.0, 1.0);
                    break;

                case "RainbowWave":
                    double wavePos = (positionFactor + (timeSeconds * speedFactor * 0.5)) % 1.0;
                    if (config.Direction == EffectDirection.RightToLeft)
                        wavePos = (1.0 - positionFactor + (timeSeconds * speedFactor * 0.5)) % 1.0;
                    baseColor = RGBColorUtilities.ColorFromHSV(wavePos * 360.0, 1.0, 1.0);
                    break;

                case "RainbowGradient":
                    double gradHue = (positionFactor * 360.0 + timeSeconds * 30.0) % 360.0;
                    baseColor = RGBColorUtilities.ColorFromHSV(gradHue, 0.9, 1.0);
                    break;

                case "ColorCycle":
                    Color[] cyclePalette = [config.PrimaryColor, config.SecondaryColor, config.AccentColor];
                    baseColor = RGBColorUtilities.InterpolateMulti(cyclePalette, progress);
                    break;

                case "DualGradient":
                    baseColor = RGBColorUtilities.Interpolate(config.PrimaryColor, config.SecondaryColor, positionFactor);
                    break;

                case "MultiGradient":
                    Color[] triPalette = [config.PrimaryColor, config.SecondaryColor, config.AccentColor];
                    baseColor = RGBColorUtilities.InterpolateMulti(triPalette, positionFactor);
                    break;

                case "ColorFade":
                    double fade = (Math.Sin(timeSeconds * speedFactor * 2.0) + 1.0) / 2.0;
                    baseColor = RGBColorUtilities.Interpolate(config.PrimaryColor, config.SecondaryColor, fade);
                    break;

                case "Alternating":
                    bool isEven = (keyIndex % 2 == 0) ^ ((int)(timeSeconds * speedFactor * 2) % 2 == 0);
                    baseColor = isEven ? config.PrimaryColor : config.SecondaryColor;
                    break;

                case "Comet":
                case "Scanner":
                    double scanPos = (Math.Sin(timeSeconds * speedFactor * 3.0) + 1.0) / 2.0;
                    double dist = Math.Abs(positionFactor - scanPos);
                    double beam = Math.Max(0.0, 1.0 - (dist * 5.0));
                    baseColor = RGBColorUtilities.Interpolate(config.BackgroundColor, config.PrimaryColor, beam);
                    break;

                case "Fire":
                    // Flickering flame noise
                    double flicker = Math.Sin(keyIndex * 13.0 + timeSeconds * 12.0) * 0.3 + 0.7;
                    Color flameYellow = Color.FromArgb(255, (int)(200 * flicker), 0);
                    Color flameRed = Color.FromArgb((int)(255 * flicker), 20, 0);
                    baseColor = RGBColorUtilities.Interpolate(flameRed, flameYellow, Math.Clamp(flicker, 0, 1));
                    break;

                case "CyberpunkMatrix":
                    double matrixRain = Math.Sin(keyIndex * 19.7 + timeSeconds * 10.0);
                    baseColor = matrixRain > 0.6 ? Color.FromArgb(0, 255, 128) : Color.FromArgb(0, 40, 20);
                    break;

                case "Aurora":
                    double wave1 = Math.Sin(positionFactor * 4.0 + timeSeconds * 1.5);
                    double wave2 = Math.Cos(positionFactor * 2.0 - timeSeconds * 2.0);
                    Color aurora1 = Color.FromArgb(0, 255, 180);
                    Color aurora2 = Color.FromArgb(140, 0, 255);
                    baseColor = RGBColorUtilities.Interpolate(aurora1, aurora2, (wave1 + wave2 + 2.0) / 4.0);
                    break;

                case "CpuTempRGB":
                case "GpuTempRGB":
                    // Hardware metric: 0.0 (30C) to 1.0 (95C)
                    if (hardwareMetric < 0.4)
                        baseColor = RGBColorUtilities.Interpolate(Color.LimeGreen, Color.Yellow, hardwareMetric / 0.4);
                    else
                        baseColor = RGBColorUtilities.Interpolate(Color.Yellow, Color.Red, (hardwareMetric - 0.4) / 0.6);
                    break;

                case "BatteryLevelRGB":
                    // Hardware metric: battery 0.0 to 1.0
                    if (positionFactor <= hardwareMetric)
                        baseColor = hardwareMetric > 0.2 ? Color.SpringGreen : Color.OrangeRed;
                    else
                        baseColor = Color.FromArgb(20, 20, 20);
                    break;

                case "AudioReactive":
                    double volume = Math.Clamp(hardwareMetric, 0.0, 1.0);
                    if (positionFactor <= volume)
                        baseColor = RGBColorUtilities.Interpolate(Color.Cyan, Color.HotPink, positionFactor);
                    else
                        baseColor = Color.FromArgb(15, 15, 25);
                    break;

                default:
                    baseColor = config.PrimaryColor;
                    break;
            }

            return RGBColorUtilities.ApplyBrightness(baseColor, config.Brightness);
        }
    }
}
