using System.Drawing;
using System.Globalization;

namespace GHelper.RGBStudio
{
    public static class RGBColorUtilities
    {
        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.White;
            hex = hex.Trim().TrimStart('#');

            if (hex.Length == 6 &&
                int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out int r) &&
                int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out int g) &&
                int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out int b))
            {
                return Color.FromArgb(r, g, b);
            }

            return Color.White;
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            v = Math.Clamp(v, 0, 255);
            p = Math.Clamp(p, 0, 255);
            q = Math.Clamp(q, 0, 255);
            t = Math.Clamp(t, 0, 255);

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }

        public static Color Interpolate(Color c1, Color c2, double fraction)
        {
            fraction = Math.Clamp(fraction, 0.0, 1.0);
            int r = (int)(c1.R + (c2.R - c1.R) * fraction);
            int g = (int)(c1.G + (c2.G - c1.G) * fraction);
            int b = (int)(c1.B + (c2.B - c1.B) * fraction);
            return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
        }

        public static Color InterpolateMulti(Color[] colors, double fraction)
        {
            if (colors == null || colors.Length == 0) return Color.White;
            if (colors.Length == 1) return colors[0];

            fraction = Math.Clamp(fraction, 0.0, 1.0);
            double scaled = fraction * (colors.Length - 1);
            int index = (int)scaled;
            double localFraction = scaled - index;

            if (index >= colors.Length - 1) return colors[^1];
            return Interpolate(colors[index], colors[index + 1], localFraction);
        }

        public static Color ApplyBrightness(Color color, int brightnessPercent)
        {
            double factor = Math.Clamp(brightnessPercent, 0, 100) / 100.0;
            return Color.FromArgb(
                (int)(color.R * factor),
                (int)(color.G * factor),
                (int)(color.B * factor));
        }
    }
}
