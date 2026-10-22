using System.Drawing.Drawing2D;
using GHelper.Helpers;

namespace GHelper.RGBStudio
{
    public class KeyRect
    {
        public int Index { get; set; }
        public string Label { get; set; } = "";
        public RectangleF Bounds { get; set; }
        public bool IsSelected { get; set; }
    }

    public class LightingPreviewCanvas : Control
    {
        private readonly List<KeyRect> _keys = [];
        private readonly List<KeyRect> _mouseZones = [];
        public RGBDeviceType DisplayDeviceType { get; set; } = RGBDeviceType.AsusLaptopKeyboard;
        public RGBEffectConfig EffectConfig { get; set; } = new();
        public HashSet<int> SelectedKeyIndices { get; } = [];

        public event EventHandler? SelectionChanged;

        public LightingPreviewCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.FromArgb(18, 18, 24);
            BuildKeyboardLayout();
            BuildMouseLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            BuildKeyboardLayout();
            BuildMouseLayout();
            Invalidate();
        }

        private void BuildKeyboardLayout()
        {
            _keys.Clear();
            float padX = 20;
            float padY = 20;
            float totalW = Width - (padX * 2);
            float totalH = Height - (padY * 2);

            int cols = 15;
            int rows = 5;
            float keyW = Math.Max(18, totalW / cols);
            float keyH = Math.Max(18, totalH / rows);

            string[][] keyLabels =
            [
                ["Esc", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "+", "Bksp", "Del"],
                ["Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", "Home"],
                ["Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter", "Enter", "PgUp"],
                ["Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift", "▲", "PgDn", "End"],
                ["Ctrl", "Win", "Alt", "Space", "Space", "Space", "Space", "Alt", "Fn", "Ctrl", "◄", "▼", "►", "Ins", "M1"]
            ];

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    string label = (r < keyLabels.Length && c < keyLabels[r].Length) ? keyLabels[r][c] : $"{idx}";
                    var rect = new RectangleF(padX + (c * keyW), padY + (r * keyH), keyW - 3, keyH - 3);
                    _keys.Add(new KeyRect
                    {
                        Index = idx++,
                        Label = label,
                        Bounds = rect
                    });
                }
            }
        }

        private void BuildMouseLayout()
        {
            _mouseZones.Clear();
            float centerX = Width / 2f;
            float centerY = Height / 2f;

            // Zone 0: Logo
            _mouseZones.Add(new KeyRect
            {
                Index = 0,
                Label = "Logo",
                Bounds = new RectangleF(centerX - 24, centerY + 20, 48, 48)
            });

            // Zone 1: Scroll Wheel
            _mouseZones.Add(new KeyRect
            {
                Index = 1,
                Label = "Scroll",
                Bounds = new RectangleF(centerX - 10, centerY - 65, 20, 36)
            });

            // Zone 2: Side Left Strip
            _mouseZones.Add(new KeyRect
            {
                Index = 2,
                Label = "Left Strip",
                Bounds = new RectangleF(centerX - 55, centerY - 30, 12, 80)
            });

            // Zone 3: Side Right Strip
            _mouseZones.Add(new KeyRect
            {
                Index = 3,
                Label = "Right Strip",
                Bounds = new RectangleF(centerX + 43, centerY - 30, 12, 80)
            });
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            var targets = DisplayDeviceType == RGBDeviceType.KreoMouse ? _mouseZones : _keys;
            foreach (var k in targets)
            {
                if (k.Bounds.Contains(e.Location))
                {
                    if (ModifierKeys == Keys.Control || ModifierKeys == Keys.Shift)
                    {
                        if (SelectedKeyIndices.Contains(k.Index))
                            SelectedKeyIndices.Remove(k.Index);
                        else
                            SelectedKeyIndices.Add(k.Index);
                    }
                    else
                    {
                        SelectedKeyIndices.Clear();
                        SelectedKeyIndices.Add(k.Index);
                    }

                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    break;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double time = RGBDeviceManager.Instance.CurrentAnimationTime;

            if (DisplayDeviceType == RGBDeviceType.KreoMouse)
            {
                PaintMousePreview(g, time);
            }
            else
            {
                PaintKeyboardPreview(g, time);
            }
        }

        private void PaintKeyboardPreview(Graphics g, double time)
        {
            int total = _keys.Count;
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(230, 230, 240));

            foreach (var k in _keys)
            {
                Color keyColor = RGBEffectsLibrary.ComputeColor(EffectConfig, k.Index, total, time);
                bool isSel = SelectedKeyIndices.Contains(k.Index);

                // Draw key base
                using var keyBrush = new SolidBrush(Color.FromArgb(25, 25, 33));
                g.FillRoundedRectangle(keyBrush, Rectangle.Round(k.Bounds), 3);

                // Draw key top glow
                using var glowBrush = new SolidBrush(Color.FromArgb(isSel ? 255 : 180, keyColor));
                var glowRect = new RectangleF(k.Bounds.X + 2, k.Bounds.Y + 2, k.Bounds.Width - 4, k.Bounds.Height - 4);
                g.FillRoundedRectangle(glowBrush, Rectangle.Round(glowRect), 2);

                // Selection highlight
                if (isSel)
                {
                    using var selPen = new Pen(Color.White, 2f);
                    g.DrawRectangle(selPen, Rectangle.Round(k.Bounds));
                }

                // Label
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(k.Label, font, textBrush, k.Bounds, sf);
            }
        }

        private void PaintMousePreview(Graphics g, double time)
        {
            float centerX = Width / 2f;
            float centerY = Height / 2f;

            // Draw mouse body silhouette
            using var mouseBodyBrush = new SolidBrush(Color.FromArgb(26, 26, 36));
            using var mouseBorderPen = new Pen(Color.FromArgb(50, 50, 70), 2f);
            var bodyRect = new RectangleF(centerX - 60, centerY - 90, 120, 180);
            var rBody = Rectangle.Round(bodyRect);
            using var mousePath = Drawing.RoundedRect(rBody, 35);
            g.FillPath(mouseBodyBrush, mousePath);
            g.DrawPath(mouseBorderPen, mousePath);

            // Draw center seam
            using var seamPen = new Pen(Color.FromArgb(40, 40, 55), 1.5f);
            g.DrawLine(seamPen, centerX, centerY - 90, centerX, centerY - 20);

            // Draw mouse zones
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(240, 240, 255));

            foreach (var mz in _mouseZones)
            {
                Color zoneColor = RGBEffectsLibrary.ComputeColor(EffectConfig, mz.Index, _mouseZones.Count, time);
                bool isSel = SelectedKeyIndices.Contains(mz.Index);

                using var zoneBrush = new SolidBrush(Color.FromArgb(isSel ? 255 : 200, zoneColor));
                g.FillRoundedRectangle(zoneBrush, Rectangle.Round(mz.Bounds), 4);

                if (isSel)
                {
                    using var selPen = new Pen(Color.White, 2f);
                    g.DrawRectangle(selPen, Rectangle.Round(mz.Bounds));
                }

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(mz.Label, font, textBrush, mz.Bounds, sf);
            }
        }

        public void SelectAll()
        {
            SelectedKeyIndices.Clear();
            var targets = DisplayDeviceType == RGBDeviceType.KreoMouse ? _mouseZones : _keys;
            foreach (var k in targets) SelectedKeyIndices.Add(k.Index);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void ClearSelection()
        {
            SelectedKeyIndices.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}
