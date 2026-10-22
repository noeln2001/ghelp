using GHelper.Helpers;
using GHelper.UI;

namespace GHelper.RGBStudio
{
    public class RGBStudioForm : RForm
    {
        private readonly RGBDeviceManager _manager = RGBDeviceManager.Instance;
        private LightingPreviewCanvas _canvas = default!;

        private ComboBox _comboDevice = default!;
        private ComboBox _comboEffects = default!;
        private ComboBox _comboThemes = default!;

        private Button _btnPrimaryColor = default!;
        private Button _btnSecondaryColor = default!;
        private Button _btnAccentColor = default!;
        private Button _btnBgColor = default!;
        private TextBox _textHex = default!;

        private Slider _sliderBrightness = default!;
        private Slider _sliderSpeed = default!;

        private Label _labelStatus = default!;
        private Label _labelHardwareBadge = default!;

        private RButton _btnApply = default!;
        private RButton _btnTurnOff = default!;
        private RButton _btnExportTheme = default!;
        private RButton _btnImportTheme = default!;
        private RButton _btnPaintSelection = default!;
        private RButton _btnSelectAll = default!;
        private RButton _btnClearSelection = default!;

        public RGBStudioForm()
        {
            InitializeComponent();
            InitTheme(true);

            _manager.FrameUpdated += (s, e) => _canvas.Invalidate();
            UpdateHardwareBadge();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper — RGB Studio Pro";
            Size = new Size(1060, 680);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(950, 600);
            BackColor = Color.FromArgb(16, 16, 22);
            ForeColor = Color.White;

            // Header Panel
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(15, 12, 15, 12),
                BackColor = Color.FromArgb(22, 22, 30)
            };

            var labelTitle = new Label
            {
                Text = "RGB STUDIO PRO",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 200),
                AutoSize = true,
                Location = new Point(15, 18)
            };

            _comboDevice = new RComboBox
            {
                Location = new Point(220, 15),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _comboDevice.Items.AddRange(["ASUS ROG Strix G16 Keyboard", "Kreo Gaming Keyboard", "Kreo Gaming Mouse", "Synchronize All Devices (Sync Mode)"]);
            _comboDevice.SelectedIndex = 0;
            _comboDevice.SelectedIndexChanged += ComboDevice_SelectedIndexChanged;

            _labelHardwareBadge = new Label
            {
                AutoSize = true,
                Location = new Point(500, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.LimeGreen,
                Text = "Hardware Detected"
            };

            panelHeader.Controls.AddRange([labelTitle, _comboDevice, _labelHardwareBadge]);
            Controls.Add(panelHeader);

            // Right Settings Panel
            var panelRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(22, 22, 30),
                AutoScroll = true
            };
            Controls.Add(panelRight);

            // Center Canvas
            _canvas = new LightingPreviewCanvas
            {
                Dock = DockStyle.Fill,
                DisplayDeviceType = RGBDeviceType.AsusLaptopKeyboard,
                EffectConfig = _manager.CurrentEffectConfig
            };
            _canvas.SelectionChanged += (s, e) =>
            {
                _labelStatus.Text = $"Selected keys: {_canvas.SelectedKeyIndices.Count}";
            };
            Controls.Add(_canvas);

            // Bottom Actions Bar
            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(15, 8, 15, 8),
                BackColor = Color.FromArgb(22, 22, 30)
            };

            _labelStatus = new Label
            {
                Location = new Point(15, 16),
                AutoSize = true,
                ForeColor = Color.LightGray,
                Text = "Ready — Previewing Rainbow Wave"
            };

            _btnApply = new RButton
            {
                Text = "Apply to Hardware",
                Size = new Size(160, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(panelBottom.Width - 175, 9),
                Activated = true
            };
            _btnApply.Click += BtnApply_Click;

            _btnTurnOff = new RButton
            {
                Text = "Turn Off",
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(panelBottom.Width - 285, 9),
                Secondary = true
            };
            _btnTurnOff.Click += (s, e) =>
            {
                _manager.CurrentEffectConfig.Brightness = 0;
                _manager.ApplyConfigToAll(_manager.CurrentEffectConfig);
                _canvas.Invalidate();
                _labelStatus.Text = "Lighting turned off";
            };

            panelBottom.Controls.AddRange([_labelStatus, _btnApply, _btnTurnOff]);
            Controls.Add(panelBottom);

            // Build Right Panel Controls
            BuildRightControls(panelRight);
        }

        private void BuildRightControls(Panel parent)
        {
            int y = 10;

            // Preset Themes
            var lblThemes = new Label { Text = "PRESET THEMES", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(10, y) };
            parent.Controls.Add(lblThemes);
            y += 24;

            _comboThemes = new RComboBox { Location = new Point(10, y), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var t in RGBThemeManager.BuiltInThemes) _comboThemes.Items.Add(t.Name);
            _comboThemes.SelectedIndex = 0;
            _comboThemes.SelectedIndexChanged += ComboThemes_SelectedIndexChanged;
            parent.Controls.Add(_comboThemes);
            y += 38;

            // Effects Library
            var lblEffects = new Label { Text = "LIGHTING EFFECTS (37)", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(10, y) };
            parent.Controls.Add(lblEffects);
            y += 24;

            _comboEffects = new RComboBox { Location = new Point(10, y), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var eff in RGBEffectsLibrary.AllEffects) _comboEffects.Items.Add(eff.Name);
            _comboEffects.SelectedIndex = 3; // Rainbow Wave
            _comboEffects.SelectedIndexChanged += ComboEffects_SelectedIndexChanged;
            parent.Controls.Add(_comboEffects);
            y += 42;

            // Colors
            var lblColors = new Label { Text = "COLOR PALETTE", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(10, y) };
            parent.Controls.Add(lblColors);
            y += 24;

            _btnPrimaryColor = CreateColorButton(Color.Cyan, 10, y, "Primary");
            _btnSecondaryColor = CreateColorButton(Color.Magenta, 90, y, "Secondary");
            _btnAccentColor = CreateColorButton(Color.Yellow, 170, y, "Accent");
            _btnBgColor = CreateColorButton(Color.Black, 250, y, "BG");
            parent.Controls.AddRange([_btnPrimaryColor, _btnSecondaryColor, _btnAccentColor, _btnBgColor]);
            y += 50;

            var lblHex = new Label { Text = "Hex Code:", Location = new Point(10, y + 4), AutoSize = true, ForeColor = Color.LightGray };
            _textHex = new RTextBox { Location = new Point(80, y), Width = 110, Text = "#00FFFF" };
            _textHex.TextChanged += (s, e) =>
            {
                var c = RGBColorUtilities.HexToColor(_textHex.Text);
                _btnPrimaryColor.BackColor = c;
                _manager.CurrentEffectConfig.PrimaryColor = c;
            };
            parent.Controls.AddRange([lblHex, _textHex]);
            y += 38;

            // Brightness & Speed Sliders
            var lblBrightness = new Label { Text = "Brightness", Location = new Point(10, y), AutoSize = true, ForeColor = Color.LightGray };
            parent.Controls.Add(lblBrightness);
            y += 20;

            _sliderBrightness = new Slider { Location = new Point(10, y), Width = 320, Height = 28, Min = 0, Max = 100, Value = 100 };
            _sliderBrightness.ValueChanged += (s, e) =>
            {
                _manager.CurrentEffectConfig.Brightness = _sliderBrightness.Value;
                _canvas.Invalidate();
            };
            parent.Controls.Add(_sliderBrightness);
            y += 34;

            var lblSpeed = new Label { Text = "Speed", Location = new Point(10, y), AutoSize = true, ForeColor = Color.LightGray };
            parent.Controls.Add(lblSpeed);
            y += 20;

            _sliderSpeed = new Slider { Location = new Point(10, y), Width = 320, Height = 28, Min = 1, Max = 100, Value = 50 };
            _sliderSpeed.ValueChanged += (s, e) =>
            {
                _manager.CurrentEffectConfig.Speed = _sliderSpeed.Value;
                _canvas.Invalidate();
            };
            parent.Controls.Add(_sliderSpeed);
            y += 36;

            // Per-Key Actions
            var lblPerKey = new Label { Text = "PER-KEY / ZONE MATRIX", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(10, y) };
            parent.Controls.Add(lblPerKey);
            y += 24;

            _btnPaintSelection = new RButton { Text = "Paint Selected", Location = new Point(10, y), Size = new Size(102, 30), Secondary = true };
            _btnPaintSelection.Click += (s, e) =>
            {
                foreach (var k in _canvas.SelectedKeyIndices)
                {
                    _manager.CurrentEffectConfig.PerKeyColors[k] = _manager.CurrentEffectConfig.PrimaryColor;
                }
                _canvas.Invalidate();
            };

            _btnSelectAll = new RButton { Text = "Select All", Location = new Point(118, y), Size = new Size(100, 30), Secondary = true };
            _btnSelectAll.Click += (s, e) => _canvas.SelectAll();

            _btnClearSelection = new RButton { Text = "Clear", Location = new Point(224, y), Size = new Size(100, 30), Secondary = true };
            _btnClearSelection.Click += (s, e) => _canvas.ClearSelection();

            parent.Controls.AddRange([_btnPaintSelection, _btnSelectAll, _btnClearSelection]);
            y += 42;

            // Import / Export
            _btnExportTheme = new RButton { Text = "Export Theme", Location = new Point(10, y), Size = new Size(150, 32), Secondary = true };
            _btnExportTheme.Click += BtnExportTheme_Click;

            _btnImportTheme = new RButton { Text = "Import Theme", Location = new Point(170, y), Size = new Size(150, 32), Secondary = true };
            _btnImportTheme.Click += BtnImportTheme_Click;

            parent.Controls.AddRange([_btnExportTheme, _btnImportTheme]);
        }

        private Button CreateColorButton(Color initColor, int x, int y, string name)
        {
            var btn = new Button
            {
                BackColor = initColor,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y),
                Size = new Size(70, 32),
                Text = name,
                ForeColor = initColor.R + initColor.G + initColor.B > 380 ? Color.Black : Color.White,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);

            btn.Click += (s, e) =>
            {
                using var cd = new ColorDialog { Color = btn.BackColor };
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    btn.BackColor = cd.Color;
                    btn.ForeColor = cd.Color.R + cd.Color.G + cd.Color.B > 380 ? Color.Black : Color.White;

                    if (name == "Primary")
                    {
                        _manager.CurrentEffectConfig.PrimaryColor = cd.Color;
                        _textHex.Text = RGBColorUtilities.ColorToHex(cd.Color);
                    }
                    else if (name == "Secondary")
                    {
                        _manager.CurrentEffectConfig.SecondaryColor = cd.Color;
                    }
                    else if (name == "Accent")
                    {
                        _manager.CurrentEffectConfig.AccentColor = cd.Color;
                    }
                    else if (name == "BG")
                    {
                        _manager.CurrentEffectConfig.BackgroundColor = cd.Color;
                    }

                    _canvas.Invalidate();
                }
            };

            return btn;
        }

        private void ComboDevice_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = _comboDevice.SelectedIndex;
            if (idx == 0) // ASUS
            {
                _canvas.DisplayDeviceType = RGBDeviceType.AsusLaptopKeyboard;
                _manager.SyncAllDevices = false;
            }
            else if (idx == 1) // Kreo Keyboard
            {
                _canvas.DisplayDeviceType = RGBDeviceType.KreoKeyboard;
                _manager.SyncAllDevices = false;
            }
            else if (idx == 2) // Kreo Mouse
            {
                _canvas.DisplayDeviceType = RGBDeviceType.KreoMouse;
                _manager.SyncAllDevices = false;
            }
            else // Sync All
            {
                _manager.SyncAllDevices = true;
            }

            UpdateHardwareBadge();
            _canvas.Invalidate();
        }

        private void UpdateHardwareBadge()
        {
            int idx = _comboDevice.SelectedIndex;
            if (idx == 0)
            {
                _labelHardwareBadge.Text = "ASUS ROG Strix G16 (Hardware Active)";
                _labelHardwareBadge.ForeColor = Color.LimeGreen;
            }
            else if (idx == 1)
            {
                bool phys = _manager.KreoKeyboardProvider.IsPhysicalConnected;
                _labelHardwareBadge.Text = phys ? "Kreo Keyboard (Physical USB)" : "Kreo Keyboard (Interactive Simulation)";
                _labelHardwareBadge.ForeColor = phys ? Color.LimeGreen : Color.Orange;
            }
            else if (idx == 2)
            {
                bool phys = _manager.KreoMouseProvider.IsPhysicalConnected;
                _labelHardwareBadge.Text = phys ? "Kreo Mouse (Physical USB)" : "Kreo Mouse (Interactive Simulation)";
                _labelHardwareBadge.ForeColor = phys ? Color.LimeGreen : Color.Orange;
            }
            else
            {
                _labelHardwareBadge.Text = "Unified Sync Across All Devices";
                _labelHardwareBadge.ForeColor = Color.Cyan;
            }
        }

        private void ComboEffects_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboEffects.SelectedIndex < 0) return;
            var def = RGBEffectsLibrary.AllEffects[_comboEffects.SelectedIndex];
            _manager.CurrentEffectConfig.EffectId = def.Id;
            _labelStatus.Text = $"Previewing: {def.Name} — {def.Description}";
            _canvas.Invalidate();
        }

        private void ComboThemes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboThemes.SelectedIndex < 0) return;
            var theme = RGBThemeManager.BuiltInThemes[_comboThemes.SelectedIndex];
            _manager.ApplyTheme(theme);

            _btnPrimaryColor.BackColor = theme.GetPrimaryColor();
            _btnSecondaryColor.BackColor = theme.GetSecondaryColor();
            _btnAccentColor.BackColor = theme.GetAccentColor();
            _btnBgColor.BackColor = theme.GetBackgroundColor();
            _textHex.Text = theme.PrimaryHex;
            _sliderBrightness.Value = theme.Brightness;
            _sliderSpeed.Value = theme.Speed;

            // Match effect dropdown
            for (int i = 0; i < RGBEffectsLibrary.AllEffects.Count; i++)
            {
                if (RGBEffectsLibrary.AllEffects[i].Id == theme.EffectId)
                {
                    _comboEffects.SelectedIndex = i;
                    break;
                }
            }

            _labelStatus.Text = $"Theme Loaded: {theme.Name} — {theme.Description}";
            _canvas.Invalidate();
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            _manager.ApplyConfigToAll(_manager.CurrentEffectConfig);
            Program.toast.RunToast("RGB Applied to Hardware", ToastIcon.BrightnessUp);
            _labelStatus.Text = "Applied successfully to connected hardware!";
        }

        private void BtnExportTheme_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog();
            sfd.Filter = "G-Helper RGB Theme (*.rgbt)|*.rgbt|JSON (*.json)|*.json";
            sfd.FileName = $"{_comboThemes.Text}_Theme.rgbt";
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                var theme = RGBTheme.FromConfig(_comboThemes.Text, _manager.CurrentEffectConfig);
                if (RGBThemeManager.ExportThemeToFile(theme, sfd.FileName))
                {
                    MessageBox.Show(this, "RGB Theme successfully exported!", "RGB Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnImportTheme_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "G-Helper RGB Theme (*.rgbt)|*.rgbt|JSON (*.json)|*.json";
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                var theme = RGBThemeManager.ImportThemeFromFile(ofd.FileName);
                if (theme != null)
                {
                    _manager.ApplyTheme(theme);
                    _comboThemes.Items.Add(theme.Name);
                    _comboThemes.SelectedItem = theme.Name;
                    _canvas.Invalidate();
                    MessageBox.Show(this, $"Theme '{theme.Name}' successfully imported and activated!", "RGB Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
