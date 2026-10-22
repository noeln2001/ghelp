using System.Diagnostics;
using GHelper.Helpers;
using GHelper.UI;

namespace GHelper.Display
{
    public class MonitorControlForm : Form
    {
        private List<ConnectedMonitorInfo> _monitors = [];
        private TabControl _tabMonitors = null!;
        private CheckBox _chkSyncAll = null!;
        private TrackBar _trackMasterBrightness = null!;
        private Label _lblMasterValue = null!;
        private RButton _btnRefresh = null!;

        public MonitorControlForm()
        {
            InitializeComponent();
            LoadMonitors();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper Ultimate — Connected Monitors & Display Manager";
            Size = new Size(950, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(16, 18, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Icon = Program.settingsForm?.Icon;

            // Top Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "CONNECTED MONITORS & DDC/CI CONTROL",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                AutoSize = true,
                Location = new Point(16, 10)
            };

            Label lblSubtitle = new Label
            {
                Text = "Direct hardware brightness, contrast, audio volume, input source, and power controls via DDC/CI",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 168, 180),
                AutoSize = true,
                Location = new Point(16, 34)
            };

            _btnRefresh = new RButton
            {
                Text = "Scan Displays",
                Size = new Size(120, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 140, 12),
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White
            };
            _btnRefresh.Click += (_, _) => LoadMonitors();

            // Master Synchronizer Row
            Panel pnlMaster = new Panel
            {
                Location = new Point(16, 60),
                Size = new Size(900, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _chkSyncAll = new CheckBox
            {
                Text = "Sync All Monitors (Master Brightness):",
                AutoSize = true,
                Location = new Point(0, 10),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Checked = AppConfig.Is("sync_external_brightness")
            };
            _chkSyncAll.CheckedChanged += (s, e) =>
            {
                AppConfig.Set("sync_external_brightness", _chkSyncAll.Checked ? 1 : 0);
                _trackMasterBrightness.Enabled = _chkSyncAll.Checked;
            };

            _trackMasterBrightness = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                TickFrequency = 10,
                Size = new Size(300, 30),
                Location = new Point(280, 6),
                Enabled = _chkSyncAll.Checked
            };
            _trackMasterBrightness.Scroll += (_, _) =>
            {
                _lblMasterValue.Text = $"{_trackMasterBrightness.Value}%";
                DdcMonitorManager.SetAllBrightness(_trackMasterBrightness.Value);
                UpdateChildSliderValues(_trackMasterBrightness.Value);
            };

            _lblMasterValue = new Label
            {
                Text = $"{_trackMasterBrightness.Value}%",
                AutoSize = true,
                Location = new Point(590, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White
            };

            pnlMaster.Controls.AddRange([_chkSyncAll, _trackMasterBrightness, _lblMasterValue]);
            pnlHeader.Controls.AddRange([lblTitle, lblSubtitle, _btnRefresh, pnlMaster]);

            // Bottom Actions Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblHint = new Label
            {
                Text = "Tip: Ensure DDC/CI is turned ON in your external monitor's built-in OSD settings menu.",
                ForeColor = Color.FromArgb(140, 150, 165),
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(16, 16)
            };

            RButton btnClose = new RButton
            {
                Text = "Close",
                Size = new Size(100, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 120, 10),
                BackColor = Color.FromArgb(45, 52, 68),
                ForeColor = Color.White
            };
            btnClose.Click += (_, _) => Close();

            pnlBottom.Controls.AddRange([lblHint, btnClose]);

            // TabControl for Connected Monitors
            _tabMonitors = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 8)
            };

            Controls.Add(_tabMonitors);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        public void LoadMonitors()
        {
            try
            {
                _monitors = DdcMonitorManager.EnumerateMonitors();
                _tabMonitors.TabPages.Clear();

                if (_monitors.Count == 0)
                {
                    TabPage tabEmpty = new TabPage("No Displays Detected");
                    tabEmpty.BackColor = Color.FromArgb(16, 18, 24);
                    Label lblEmpty = new Label
                    {
                        Text = "No active monitors detected. Connect your external monitor via HDMI or Type-C DisplayPort and click Scan Displays.",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.Gray
                    };
                    tabEmpty.Controls.Add(lblEmpty);
                    _tabMonitors.TabPages.Add(tabEmpty);
                    return;
                }

                foreach (var mon in _monitors)
                {
                    string tabTitle = mon.IsInternal ? "💻 Built-in Display" : $"🖥️ {mon.FriendlyName}";
                    TabPage page = new TabPage(tabTitle);
                    page.BackColor = Color.FromArgb(18, 20, 28);
                    page.Controls.Add(CreateMonitorControlCard(mon));
                    _tabMonitors.TabPages.Add(page);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[MonitorControl] Load error: {ex.Message}");
            }
        }

        private Panel CreateMonitorControlCard(ConnectedMonitorInfo mon)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            int curY = 10;

            // Monitor Info Banner
            Panel pnlInfo = new Panel
            {
                Location = new Point(0, curY),
                Size = new Size(880, 50),
                BackColor = Color.FromArgb(24, 28, 38)
            };

            Label lblDevName = new Label
            {
                Text = mon.FriendlyName,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                AutoSize = true,
                Location = new Point(12, 6)
            };

            string statusStr = mon.IsInternal
                ? "Built-in Screen (Native WMI / ACPI)"
                : (mon.SupportsDdc ? "DDC/CI Hardware Communication: READY" : "External Display (DDC/CI Probing)");

            Label lblSpecs = new Label
            {
                Text = $"{mon.Bounds.Width} × {mon.Bounds.Height} @ {(mon.RefreshRate > 0 ? $"{mon.RefreshRate}Hz" : "60Hz")} | {(mon.IsPrimary ? "Primary Display | " : "")}{statusStr}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 170, 185),
                AutoSize = true,
                Location = new Point(12, 28)
            };

            pnlInfo.Controls.AddRange([lblDevName, lblSpecs]);
            card.Controls.Add(pnlInfo);
            curY += 65;

            // Brightness Control Group
            Label lblBrightnessTitle = new Label
            {
                Text = "BRIGHTNESS",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, curY),
                AutoSize = true
            };
            card.Controls.Add(lblBrightnessTitle);
            curY += 26;

            Panel pnlBrightness = new Panel { Location = new Point(0, curY), Size = new Size(880, 42) };
            Button btnMinusB = CreateStepButton("-5%");
            btnMinusB.Location = new Point(0, 4);

            TrackBar trackB = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Clamp(mon.CurrentBrightness, 0, 100),
                TickFrequency = 10,
                Size = new Size(500, 32),
                Location = new Point(60, 2)
            };

            Button btnPlusB = CreateStepButton("+5%");
            btnPlusB.Location = new Point(570, 4);

            Label lblValB = new Label
            {
                Text = $"{trackB.Value}%",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(640, 8),
                AutoSize = true
            };

            trackB.Scroll += (_, _) =>
            {
                lblValB.Text = $"{trackB.Value}%";
                DdcMonitorManager.SetBrightness(mon, trackB.Value);
            };

            btnMinusB.Click += (_, _) =>
            {
                trackB.Value = Math.Max(0, trackB.Value - 5);
                lblValB.Text = $"{trackB.Value}%";
                DdcMonitorManager.SetBrightness(mon, trackB.Value);
            };

            btnPlusB.Click += (_, _) =>
            {
                trackB.Value = Math.Min(100, trackB.Value + 5);
                lblValB.Text = $"{trackB.Value}%";
                DdcMonitorManager.SetBrightness(mon, trackB.Value);
            };

            pnlBrightness.Controls.AddRange([btnMinusB, trackB, btnPlusB, lblValB]);
            card.Controls.Add(pnlBrightness);
            curY += 50;

            // Contrast Control Group
            Label lblContrastTitle = new Label
            {
                Text = "CONTRAST",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, curY),
                AutoSize = true
            };
            card.Controls.Add(lblContrastTitle);
            curY += 26;

            Panel pnlContrast = new Panel { Location = new Point(0, curY), Size = new Size(880, 42) };
            Button btnMinusC = CreateStepButton("-5%");
            btnMinusC.Location = new Point(0, 4);

            TrackBar trackC = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Clamp(mon.CurrentContrast, 0, 100),
                TickFrequency = 10,
                Size = new Size(500, 32),
                Location = new Point(60, 2)
            };

            Button btnPlusC = CreateStepButton("+5%");
            btnPlusC.Location = new Point(570, 4);

            Label lblValC = new Label
            {
                Text = $"{trackC.Value}%",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(640, 8),
                AutoSize = true
            };

            trackC.Scroll += (_, _) =>
            {
                lblValC.Text = $"{trackC.Value}%";
                DdcMonitorManager.SetContrast(mon, trackC.Value);
            };

            btnMinusC.Click += (_, _) =>
            {
                trackC.Value = Math.Max(0, trackC.Value - 5);
                lblValC.Text = $"{trackC.Value}%";
                DdcMonitorManager.SetContrast(mon, trackC.Value);
            };

            btnPlusC.Click += (_, _) =>
            {
                trackC.Value = Math.Min(100, trackC.Value + 5);
                lblValC.Text = $"{trackC.Value}%";
                DdcMonitorManager.SetContrast(mon, trackC.Value);
            };

            pnlContrast.Controls.AddRange([btnMinusC, trackC, btnPlusC, lblValC]);
            card.Controls.Add(pnlContrast);
            curY += 50;

            // Audio Volume Control Group (if supported or external)
            if (!mon.IsInternal)
            {
                Label lblVolTitle = new Label
                {
                    Text = "MONITOR SPEAKER / AUDIO VOLUME",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(0, curY),
                    AutoSize = true
                };
                card.Controls.Add(lblVolTitle);
                curY += 26;

                Panel pnlVol = new Panel { Location = new Point(0, curY), Size = new Size(880, 42) };
                TrackBar trackV = new TrackBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = Math.Clamp(mon.CurrentVolume, 0, 100),
                    TickFrequency = 10,
                    Size = new Size(500, 32),
                    Location = new Point(60, 2)
                };

                Label lblValV = new Label
                {
                    Text = $"{trackV.Value}%",
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 220, 255),
                    Location = new Point(580, 8),
                    AutoSize = true
                };

                trackV.Scroll += (_, _) =>
                {
                    lblValV.Text = $"{trackV.Value}%";
                    DdcMonitorManager.SetVolume(mon, trackV.Value);
                };

                pnlVol.Controls.AddRange([trackV, lblValV]);
                card.Controls.Add(pnlVol);
                curY += 50;

                // Input Source & Power Mode Group
                Label lblInputTitle = new Label
                {
                    Text = "INPUT SOURCE & POWER CONTROLS",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(0, curY),
                    AutoSize = true
                };
                card.Controls.Add(lblInputTitle);
                curY += 26;

                Panel pnlInput = new Panel { Location = new Point(0, curY), Size = new Size(880, 42) };
                Label lblInput = new Label { Text = "Switch Input:", AutoSize = true, Location = new Point(0, 8) };

                ComboBox comboInput = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(90, 4),
                    Size = new Size(160, 28),
                    BackColor = Color.FromArgb(32, 38, 50),
                    ForeColor = Color.White
                };
                comboInput.Items.AddRange(["DisplayPort 1", "DisplayPort 2", "HDMI 1", "HDMI 2", "USB-C / Type-C"]);
                comboInput.SelectedIndex = 0;
                comboInput.SelectedIndexChanged += (_, _) =>
                {
                    int code = comboInput.SelectedIndex switch
                    {
                        0 => 0x0F,
                        1 => 0x10,
                        2 => 0x11,
                        3 => 0x12,
                        4 => 0x1B,
                        _ => 0x0F
                    };
                    DdcMonitorManager.SetInputSource(mon, code);
                };

                RButton btnStandby = new RButton
                {
                    Text = "Standby Display",
                    Size = new Size(130, 30),
                    Location = new Point(270, 3),
                    BackColor = Color.FromArgb(42, 48, 64),
                    ForeColor = Color.White
                };
                btnStandby.Click += (_, _) => DdcMonitorManager.SetPowerMode(mon, DdcNative.POWER_STANDBY);

                RButton btnPowerOff = new RButton
                {
                    Text = "Turn Screen Off",
                    Size = new Size(130, 30),
                    Location = new Point(410, 3),
                    BackColor = Color.FromArgb(70, 30, 35),
                    ForeColor = Color.White
                };
                btnPowerOff.Click += (_, _) => DdcMonitorManager.SetPowerMode(mon, DdcNative.POWER_OFF);

                pnlInput.Controls.AddRange([lblInput, comboInput, btnStandby, btnPowerOff]);
                card.Controls.Add(pnlInput);
                curY += 50;
            }

            // Quick Presets
            Label lblPresetsTitle = new Label
            {
                Text = "QUICK DISPLAY PRESETS",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, curY),
                AutoSize = true
            };
            card.Controls.Add(lblPresetsTitle);
            curY += 28;

            Panel pnlPresets = new Panel { Location = new Point(0, curY), Size = new Size(880, 44) };
            (string Name, int B, int C, Color Clr)[] presets =
            [
                ("🎮 Gaming (100% / 75%)", 100, 75, Color.FromArgb(200, 30, 60)),
                ("💼 Work (75% / 50%)", 75, 50, Color.FromArgb(0, 130, 200)),
                ("🌙 Night Mode (25% / 40%)", 25, 40, Color.FromArgb(120, 60, 180)),
                ("📖 Reading (15% / 30%)", 15, 30, Color.FromArgb(60, 120, 60))
            ];

            int pX = 0;
            foreach (var (pName, pB, pC, pColor) in presets)
            {
                RButton btnP = new RButton
                {
                    Text = pName,
                    Size = new Size(190, 36),
                    Location = new Point(pX, 0),
                    BackColor = pColor,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                };
                btnP.Click += (_, _) =>
                {
                    trackB.Value = pB;
                    lblValB.Text = $"{pB}%";
                    DdcMonitorManager.SetBrightness(mon, pB);

                    trackC.Value = pC;
                    lblValC.Text = $"{pC}%";
                    DdcMonitorManager.SetContrast(mon, pC);
                };
                pnlPresets.Controls.Add(btnP);
                pX += 205;
            }

            card.Controls.Add(pnlPresets);
            return card;
        }

        private static Button CreateStepButton(string text)
        {
            return new Button
            {
                Text = text,
                Size = new Size(50, 28),
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
        }

        private void UpdateChildSliderValues(int value)
        {
            // Updates sliders across active controls
            foreach (TabPage page in _tabMonitors.TabPages)
            {
                foreach (Control c in page.Controls)
                {
                    if (c is Panel card)
                    {
                        foreach (Control sub in card.Controls)
                        {
                            if (sub is Panel row)
                            {
                                foreach (Control elem in row.Controls)
                                {
                                    if (elem is TrackBar tb && tb.Maximum == 100 && tb.Location.X == 60)
                                    {
                                        tb.Value = value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
