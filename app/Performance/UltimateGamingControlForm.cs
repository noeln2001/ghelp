using System.Drawing;
using GHelper.Display;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Network;
using GHelper.RGBStudio;
using GHelper.UI;

namespace GHelper.Performance
{
    public class UltimateGamingControlForm : Form
    {
        private Label _lblStatusTimer = null!;
        private Label _lblStatusPCore = null!;
        private Label _lblStatusNetwork = null!;
        private Label _lblMemorySaved = null!;
        private RButton _btnTimer = null!;
        private RButton _btnPCore = null!;
        private RButton _btnHighPriority = null!;
        private RButton _btnOptimizeNetwork = null!;
        private RButton _btnRestoreNetwork = null!;
        private RButton _btnRapidFire = null!;
        private RButton _btnScreenOverdrive = null!;
        private ComboBox _comboCps = null!;
        private List<RButton> _reactiveButtons = [];

        public UltimateGamingControlForm()
        {
            InitializeComponent();
            RefreshAllStatus();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper Ultimate — Gaming Performance & Latency Optimizer";
            Size = new Size(980, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(16, 18, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Icon = Program.settingsForm?.Icon;

            // Top Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(20, 12, 20, 12)
            };

            Label lblTitle = new Label
            {
                Text = "GAMING PERFORMANCE & LOW-LATENCY OPTIMIZER",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 180),
                AutoSize = true,
                Location = new Point(16, 12)
            };

            Label lblSubtitle = new Label
            {
                Text = "Intel 14th-Gen i9 P-Core Affinity, 0.5ms Timer, TCP NoDelay Ping Optimizer, Screen Ambilight, and Rapid-Fire",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 170, 185),
                AutoSize = true,
                Location = new Point(16, 38)
            };

            RButton btnFlush = new RButton
            {
                Text = "⚡ Flush Standby RAM",
                Size = new Size(160, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 180, 18),
                BackColor = Color.FromArgb(0, 120, 90),
                ForeColor = Color.White
            };
            btnFlush.Click += (_, _) =>
            {
                long freed = GamingLatencyOptimizer.FlushMemoryAndWorkingSets();
                string mbStr = (freed / (1024 * 1024)).ToString();
                _lblMemorySaved.Text = $"Last Trim Reclaimed: {mbStr} MB physical cache";
                Program.toast?.RunToast($"RAM Cache Cleaned ({mbStr} MB freed)", ToastIcon.Charger);
            };

            pnlHeader.Controls.AddRange([lblTitle, lblSubtitle, btnFlush]);

            // Bottom Actions Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            _lblMemorySaved = new Label
            {
                Text = "System Standby Cache: Ready",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 160, 180),
                Location = new Point(16, 16),
                AutoSize = true
            };

            RButton btnClose = new RButton
            {
                Text = "Close",
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 130, 10),
                BackColor = Color.FromArgb(45, 52, 68),
                ForeColor = Color.White
            };
            btnClose.Click += (_, _) => Close();

            pnlBottom.Controls.AddRange([_lblMemorySaved, btnClose]);

            // Scrollable Content
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            int curY = 10;

            // SECTION 1: Intel 14th-Gen i9 Scheduler & Core Affinity
            Label lblIntelHeader = new Label
            {
                Text = "INTEL 14TH-GEN i9 PROCESS SCHEDULER & LATENCY",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblIntelHeader);
            curY += 28;

            Panel pnlIntel = new Panel { Location = new Point(0, curY), Size = new Size(910, 80), BackColor = Color.FromArgb(22, 25, 34) };

            _lblStatusPCore = new Label
            {
                Text = "Intel Core i9 Topology: 8 P-Cores (16 Threads) + 16 E-Cores (24 Cores / 32 Threads)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 200, 230),
                Location = new Point(14, 10),
                AutoSize = true
            };

            _btnPCore = new RButton
            {
                Text = "Lock Games to P-Cores",
                Size = new Size(210, 34),
                Location = new Point(14, 34),
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.White
            };
            _btnPCore.Click += (_, _) =>
            {
                bool next = !GamingLatencyOptimizer.IsPCoreLockEnabled;
                GamingLatencyOptimizer.SetPCoreLock(next);
                RefreshAllStatus();
                Program.toast?.RunToast($"P-Core Lock: {(next ? "ON" : "OFF")}", ToastIcon.Charger);
            };

            _btnHighPriority = new RButton
            {
                Text = "Auto High Process Priority",
                Size = new Size(210, 34),
                Location = new Point(234, 34),
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.White
            };
            _btnHighPriority.Click += (_, _) =>
            {
                bool next = !GamingLatencyOptimizer.IsHighPriorityEnabled;
                GamingLatencyOptimizer.SetGameHighPriority(next);
                RefreshAllStatus();
                Program.toast?.RunToast($"Game High Priority: {(next ? "ON" : "OFF")}", ToastIcon.Charger);
            };

            _btnTimer = new RButton
            {
                Text = "0.5ms High-Precision Timer",
                Size = new Size(220, 34),
                Location = new Point(454, 34),
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.White
            };
            _btnTimer.Click += (_, _) =>
            {
                bool next = !GamingLatencyOptimizer.IsHighResTimerActive;
                GamingLatencyOptimizer.SetHighResTimer(next);
                RefreshAllStatus();
                Program.toast?.RunToast($"0.5ms Timer: {(next ? "ACTIVE" : "STANDARD")}", ToastIcon.Charger);
            };

            _lblStatusTimer = new Label
            {
                Text = "Timer: 15.6ms standard",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 150, 170),
                Location = new Point(685, 42),
                AutoSize = true
            };

            pnlIntel.Controls.AddRange([_lblStatusPCore, _btnPCore, _btnHighPriority, _btnTimer, _lblStatusTimer]);
            pnlContent.Controls.Add(pnlIntel);
            curY += 92;

            // SECTION 2: Network & Low Ping Optimizer
            Label lblNetHeader = new Label
            {
                Text = "GAMING NETWORK & LOW-PING OPTIMIZER (TCP NoDelay & QoS)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblNetHeader);
            curY += 28;

            Panel pnlNet = new Panel { Location = new Point(0, curY), Size = new Size(910, 80), BackColor = Color.FromArgb(22, 25, 34) };

            _lblStatusNetwork = new Label
            {
                Text = "Status: TCP NoDelay, Immediate ACK, and QoS Gaming Prioritization",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 200, 230),
                Location = new Point(14, 10),
                AutoSize = true
            };

            _btnOptimizeNetwork = new RButton
            {
                Text = "🚀 Apply Low-Ping Network Tweaks",
                Size = new Size(260, 34),
                Location = new Point(14, 34),
                BackColor = Color.FromArgb(28, 80, 140),
                ForeColor = Color.White
            };
            _btnOptimizeNetwork.Click += (_, _) =>
            {
                bool ok = GamingNetworkOptimizer.ApplyGamingNetworkOptimizations();
                RefreshAllStatus();
                Program.toast?.RunToast(ok ? "Gaming Network Tweaks Applied!" : "Failed to apply network tweaks", ToastIcon.Charger);
            };

            _btnRestoreNetwork = new RButton
            {
                Text = "Restore Standard Windows Network",
                Size = new Size(240, 34),
                Location = new Point(284, 34),
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.White
            };
            _btnRestoreNetwork.Click += (_, _) =>
            {
                bool ok = GamingNetworkOptimizer.RestoreDefaultNetworkOptimizations();
                RefreshAllStatus();
                Program.toast?.RunToast(ok ? "Restored Default Network" : "Failed to restore", ToastIcon.BrightnessDown);
            };

            pnlNet.Controls.AddRange([_lblStatusNetwork, _btnOptimizeNetwork, _btnRestoreNetwork]);
            pnlContent.Controls.Add(pnlNet);
            curY += 92;

            // SECTION 3: RGB Reactive & Screen Ambilight
            Label lblRgbHeader = new Label
            {
                Text = "RGB REACTIVE ENGINE (AUDIO SPECTRUM, SCREEN AMBILIGHT & THERMAL)",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblRgbHeader);
            curY += 28;

            Panel pnlReactive = new Panel { Location = new Point(0, curY), Size = new Size(910, 90), BackColor = Color.FromArgb(22, 25, 34) };

            (string Title, string Desc, ReactiveMode Mode, Color Accent)[] modes =
            [
                ("🎵 Audio Spectrum", "Music & Gunshot Beat Sync", ReactiveMode.AudioSpectrum, Color.FromArgb(0, 220, 255)),
                ("🖥️ Screen Ambilight", "Real-Time Display Edge Colors", ReactiveMode.ScreenAmbilight, Color.FromArgb(240, 160, 0)),
                ("🌡️ Thermal Health", "Green/Yellow/Red Temp Glow", ReactiveMode.ThermalHealth, Color.FromArgb(220, 40, 80)),
                ("⚪ Off / Normal", "Standard RGB Themes", ReactiveMode.Off, Color.FromArgb(120, 130, 150))
            ];

            int rX = 14;
            int rW = 210;
            foreach (var (rTitle, rDesc, rMode, rAccent) in modes)
            {
                RButton btnR = new RButton
                {
                    Text = $"{rTitle}\n{rDesc}",
                    Size = new Size(rW, 64),
                    Location = new Point(rX, 12),
                    BackColor = Color.FromArgb(28, 34, 46),
                    ForeColor = Color.White,
                    BorderRadius = 6,
                    BorderColor = rMode == RGBReactiveEngine.Instance.CurrentMode ? rAccent : Color.Transparent,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
                };

                btnR.Click += (s, e) =>
                {
                    RGBReactiveEngine.Instance.SetMode(rMode);
                    foreach (var b in _reactiveButtons) b.BorderColor = Color.Transparent;
                    btnR.BorderColor = rAccent;
                    Program.toast?.RunToast($"Reactive RGB: {rTitle}", ToastIcon.Charger);
                };

                _reactiveButtons.Add(btnR);
                pnlReactive.Controls.Add(btnR);
                rX += rW + 14;
            }

            pnlContent.Controls.Add(pnlReactive);
            curY += 102;

            // SECTION 4: Rapid-Fire Clicker & Display Overdrive
            Label lblAutomation = new Label
            {
                Text = "RAPID-FIRE GAMING AUTOMATION & ROG STRIX G16 DISPLAY",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblAutomation);
            curY += 28;

            Panel pnlAuto = new Panel { Location = new Point(0, curY), Size = new Size(910, 80), BackColor = Color.FromArgb(22, 25, 34) };

            _btnRapidFire = new RButton
            {
                Text = "Toggle Rapid-Fire Clicker",
                Size = new Size(200, 34),
                Location = new Point(14, 22),
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.White
            };
            _btnRapidFire.Click += (_, _) =>
            {
                MacroStudio.ToggleRapidFire();
                RefreshAllStatus();
            };

            Label lblCps = new Label { Text = "CPS Target:", Location = new Point(225, 30), AutoSize = true };
            _comboCps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(310, 26),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(28, 32, 44),
                ForeColor = Color.White
            };
            _comboCps.Items.AddRange(["10 CPS", "15 CPS", "25 CPS", "40 CPS"]);
            _comboCps.SelectedIndex = 1;
            _comboCps.SelectedIndexChanged += (_, _) =>
            {
                string txt = _comboCps.SelectedItem?.ToString() ?? "15";
                int val = int.TryParse(txt.Replace(" CPS", ""), out int parsed) ? parsed : 15;
                MacroStudio.TargetCPS = val;
            };

            _btnScreenOverdrive = new RButton
            {
                Text = "Toggle Display Overdrive (3ms)",
                Size = new Size(220, 34),
                Location = new Point(440, 22),
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.White
            };
            _btnScreenOverdrive.Click += (_, _) =>
            {
                int currentOd = AppConfig.Get("overdrive", 1);
                int nextOd = currentOd == 1 ? 0 : 1;
                ScreenControl.SetScreen(overdrive: nextOd);
                Program.toast?.RunToast($"Display Overdrive: {(nextOd == 1 ? "ON (3ms)" : "OFF")}", ToastIcon.Charger);
            };

            pnlAuto.Controls.AddRange([_btnRapidFire, lblCps, _comboCps, _btnScreenOverdrive]);
            pnlContent.Controls.Add(pnlAuto);

            Controls.Add(pnlContent);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        public void RefreshAllStatus()
        {
            // 1. P-Core Lock Status
            bool pCore = GamingLatencyOptimizer.IsPCoreLockEnabled;
            _btnPCore.BackColor = pCore ? Color.FromArgb(0, 140, 100) : Color.FromArgb(35, 42, 56);
            _btnPCore.Text = pCore ? "✓ P-Core Lock ACTIVE" : "Lock Games to P-Cores";

            // 2. High Priority Status
            bool prio = GamingLatencyOptimizer.IsHighPriorityEnabled;
            _btnHighPriority.BackColor = prio ? Color.FromArgb(0, 140, 100) : Color.FromArgb(35, 42, 56);
            _btnHighPriority.Text = prio ? "✓ High Priority ACTIVE" : "Auto High Process Priority";

            // 3. Timer Status
            bool timer = GamingLatencyOptimizer.IsHighResTimerActive;
            _btnTimer.BackColor = timer ? Color.FromArgb(0, 140, 100) : Color.FromArgb(35, 42, 56);
            _btnTimer.Text = timer ? "✓ 0.5ms Timer ACTIVE" : "0.5ms High-Precision Timer";
            _lblStatusTimer.Text = timer ? "Timer: 0.5ms - 1.0ms active" : "Timer: standard Windows 15.6ms";
            _lblStatusTimer.ForeColor = timer ? Color.FromArgb(0, 255, 180) : Color.FromArgb(140, 150, 170);

            // 4. Network Status
            bool net = GamingNetworkOptimizer.IsNetworkOptimized();
            _btnOptimizeNetwork.BackColor = net ? Color.FromArgb(0, 120, 80) : Color.FromArgb(28, 80, 140);
            _btnOptimizeNetwork.Text = net ? "✓ Low-Ping Mode Active" : "🚀 Apply Low-Ping Network Tweaks";
            _lblStatusNetwork.Text = net
                ? "Status: TCP NoDelay = 1 | TcpAckFrequency = 1 | Zero-Throttling (E-Sports Ready)"
                : "Status: Standard Windows TCP aggregation and packet buffering";
            _lblStatusNetwork.ForeColor = net ? Color.FromArgb(0, 255, 180) : Color.FromArgb(180, 200, 230);

            // 5. Rapid Fire Status
            bool rf = MacroStudio.IsRapidFireActive;
            _btnRapidFire.BackColor = rf ? Color.FromArgb(180, 40, 50) : Color.FromArgb(35, 42, 56);
            _btnRapidFire.Text = rf ? "✓ Rapid-Fire CLICKING" : "Toggle Rapid-Fire Clicker";
        }
    }
}
