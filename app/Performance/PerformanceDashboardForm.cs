using GHelper.Helpers;
using GHelper.Mode;
using GHelper.UI;

namespace GHelper.Performance
{
    public class PerformanceDashboardForm : RForm
    {
        private readonly BenchmarkManager _bench = BenchmarkManager.Instance;
        private System.Windows.Forms.Timer _telemetryTimer = default!;

        // Telemetry Labels
        private Label _lblCpuTemp = default!;
        private Label _lblCpuPower = default!;
        private Label _lblCpuUsage = default!;
        private Label _lblGpuTemp = default!;
        private Label _lblGpuPower = default!;
        private Label _lblGpuUsage = default!;
        private Label _lblRamUsage = default!;
        private Label _lblFanSpeed = default!;
        private Label _lblBattery = default!;

        // Benchmark Controls
        private RButton _btnRecord = default!;
        private Label _lblBenchmarkStatus = default!;
        private Label _lblFpsLive = default!;
        private Label _lblOnePercentLow = default!;
        private ListBox _listSessions = default!;
        private RButton _btnCompare = default!;
        private RButton _btnExportCsv = default!;

        public PerformanceDashboardForm()
        {
            InitializeComponent();
            InitTheme(true);

            _telemetryTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _telemetryTimer.Tick += TelemetryTimer_Tick;
            _telemetryTimer.Start();

            _bench.SampleRecorded += Bench_SampleRecorded;
            _bench.SessionCompleted += Bench_SessionCompleted;

            UpdateTelemetry();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper — Gaming Performance Center & Benchmarks";
            Size = new Size(1020, 680);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(880, 580);
            BackColor = Color.FromArgb(16, 16, 22);
            ForeColor = Color.White;

            // Header
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(15, 12, 15, 12),
                BackColor = Color.FromArgb(22, 22, 30)
            };

            var labelTitle = new Label
            {
                Text = "PERFORMANCE DASHBOARD & BENCHMARKS",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 200),
                AutoSize = true,
                Location = new Point(15, 16)
            };
            panelHeader.Controls.Add(labelTitle);
            Controls.Add(panelHeader);

            // Left: Real-time Telemetry Grid
            var panelTelemetry = new Panel
            {
                Dock = DockStyle.Left,
                Width = 460,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(20, 20, 28)
            };
            BuildTelemetryCards(panelTelemetry);
            Controls.Add(panelTelemetry);

            // Right: Benchmarking Center
            var panelBench = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(16, 16, 22)
            };
            BuildBenchmarkControls(panelBench);
            Controls.Add(panelBench);
        }

        private void BuildTelemetryCards(Panel parent)
        {
            int y = 15;
            var lblSection = new Label { Text = "HARDWARE TELEMETRY", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(15, y) };
            parent.Controls.Add(lblSection);
            y += 28;

            // CPU Card
            var cardCpu = CreateCard("CPU: 14th Gen Intel Core i9", 15, y, 420, 95);
            _lblCpuTemp = CreateMetricLabel("Temperature: --°C", 15, 30, cardCpu);
            _lblCpuUsage = CreateMetricLabel("Utilization: --%", 15, 52, cardCpu);
            _lblCpuPower = CreateMetricLabel("Package Power: -- W", 210, 30, cardCpu);
            parent.Controls.Add(cardCpu);
            y += 105;

            // GPU Card
            var cardGpu = CreateCard("GPU: NVIDIA GeForce RTX 4060 Laptop", 15, y, 420, 95);
            _lblGpuTemp = CreateMetricLabel("Temperature: --°C", 15, 30, cardGpu);
            _lblGpuUsage = CreateMetricLabel("Utilization: --%", 15, 52, cardGpu);
            _lblGpuPower = CreateMetricLabel("TGP Power: -- W", 210, 30, cardGpu);
            parent.Controls.Add(cardGpu);
            y += 105;

            // Memory & System Card
            var cardSys = CreateCard("System & Thermal Telemetry", 15, y, 420, 115);
            _lblRamUsage = CreateMetricLabel("Memory: 32 GB DDR5", 15, 30, cardSys);
            _lblFanSpeed = CreateMetricLabel("Fan Speeds: Active", 15, 55, cardSys);
            _lblBattery = CreateMetricLabel("Power: AC Connected", 15, 80, cardSys);
            parent.Controls.Add(cardSys);
        }

        private void BuildBenchmarkControls(Panel parent)
        {
            int y = 15;
            var lblSection = new Label { Text = "GAME BENCHMARK RECORDER & COMPARISON", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(15, y), AutoSize = true };
            parent.Controls.Add(lblSection);
            y += 28;

            _btnRecord = new RButton
            {
                Text = "Start Benchmark Session",
                Location = new Point(15, y),
                Size = new Size(220, 38),
                Activated = true
            };
            _btnRecord.Click += BtnRecord_Click;

            _lblBenchmarkStatus = new Label
            {
                Location = new Point(250, y + 10),
                AutoSize = true,
                ForeColor = Color.LightGray,
                Text = "Idle — Ready to benchmark"
            };

            parent.Controls.AddRange([_btnRecord, _lblBenchmarkStatus]);
            y += 50;

            // FPS Display Card
            var cardFps = CreateCard("Live Session Framerate", 15, y, 500, 85);
            _lblFpsLive = new Label { Text = "FPS: --", Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = Color.LimeGreen, Location = new Point(15, 30), AutoSize = true };
            _lblOnePercentLow = new Label { Text = "1% Low: --", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.Yellow, Location = new Point(220, 36), AutoSize = true };
            cardFps.Controls.AddRange([_lblFpsLive, _lblOnePercentLow]);
            parent.Controls.Add(cardFps);
            y += 98;

            // Session History
            var lblHistory = new Label { Text = "HISTORICAL BENCHMARK RUNS", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(15, y) };
            parent.Controls.Add(lblHistory);
            y += 22;

            _listSessions = new ListBox
            {
                Location = new Point(15, y),
                Width = 500,
                Height = 160,
                BackColor = Color.FromArgb(24, 24, 32),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9f)
            };
            parent.Controls.Add(_listSessions);
            y += 170;

            _btnCompare = new RButton { Text = "Compare Two Sessions", Location = new Point(15, y), Size = new Size(200, 34), Secondary = true };
            _btnCompare.Click += BtnCompare_Click;

            _btnExportCsv = new RButton { Text = "Export Report (CSV)", Location = new Point(225, y), Size = new Size(180, 34), Secondary = true };
            _btnExportCsv.Click += BtnExportCsv_Click;

            parent.Controls.AddRange([_btnCompare, _btnExportCsv]);
        }

        private Panel CreateCard(string title, int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(26, 26, 36),
                Padding = new Padding(10)
            };
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 200, 255),
                Location = new Point(10, 8),
                AutoSize = true
            };
            p.Controls.Add(lbl);
            return p;
        }

        private Label CreateMetricLabel(string text, int x, int y, Control parent)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Segoe UI", 9f)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private void TelemetryTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTelemetry();
        }

        private void UpdateTelemetry()
        {
            int cpuT = (int)(HardwareControl.cpuTemp ?? 0);
            int gpuT = (int)(HardwareControl.gpuTemp ?? 0);
            float cpuP = HardwareControl.cpuPower ?? 0;
            float gpuP = HardwareControl.gpuPower ?? 0;
            float cpuU = HardwareControl.cpuUsage ?? 0;
            float gpuU = HardwareControl.gpuUsage ?? 0;

            _lblCpuTemp.Text = $"Temperature: {cpuT}°C";
            _lblCpuUsage.Text = $"Utilization: {cpuU:F0}%";
            _lblCpuPower.Text = $"Package Power: {cpuP:F1} W";

            _lblGpuTemp.Text = $"Temperature: {gpuT}°C";
            _lblGpuUsage.Text = $"Utilization: {gpuU:F0}%";
            _lblGpuPower.Text = $"TGP Power: {gpuP:F1} W";

            string ramText = HardwareControl.batteryCapacity > 0 ? $"{HardwareControl.batteryCapacity:F0}%" : "100%";
            _lblBattery.Text = $"Battery: {ramText} ({(SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online ? "AC Connected" : "Discharging")})";
        }

        private void BtnRecord_Click(object? sender, EventArgs e)
        {
            if (!_bench.IsRecording)
            {
                _bench.StartBenchmark("ASUS ROG Strix Gameplay", Modes.GetCurrentName());
                _btnRecord.Text = "Stop Benchmark";
                _btnRecord.Activated = false;
                _lblBenchmarkStatus.Text = "Recording session telemetry at 10Hz...";
                _lblBenchmarkStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                var session = _bench.StopBenchmark();
                _btnRecord.Text = "Start Benchmark Session";
                _btnRecord.Activated = true;
                _lblBenchmarkStatus.Text = "Session saved!";
                _lblBenchmarkStatus.ForeColor = Color.LightGray;

                if (session != null)
                {
                    _listSessions.Items.Add($"[{session.StartTime:HH:mm:ss}] {session.PerformanceProfile} — Avg: {session.AverageFps:F1} FPS | 1%: {session.OnePercentLowFps:F1} FPS");
                }
            }
        }

        private void Bench_SampleRecorded(object? sender, EventArgs e)
        {
            if (_bench.ActiveSession != null && _bench.ActiveSession.Samples.Count > 0)
            {
                var last = _bench.ActiveSession.Samples.Last();
                _lblFpsLive.Text = $"FPS: {last.Fps:F0}";

                var fpsList = _bench.ActiveSession.Samples.Select(s => s.Fps).OrderBy(f => f).ToList();
                int onePct = Math.Max(0, (int)(fpsList.Count * 0.01));
                _lblOnePercentLow.Text = $"1% Low: {fpsList[onePct]:F1}";
            }
        }

        private void Bench_SessionCompleted(object? sender, EventArgs e)
        {
            _lblFpsLive.Text = "FPS: --";
            _lblOnePercentLow.Text = "1% Low: --";
        }

        private void BtnCompare_Click(object? sender, EventArgs e)
        {
            if (_bench.SessionHistory.Count < 2)
            {
                MessageBox.Show(this, "Please record at least two benchmark sessions to compare performance profiles.", "Benchmark Comparison", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sessionA = _bench.SessionHistory[^2];
            var sessionB = _bench.SessionHistory[^1];
            string report = BenchmarkComparison.CompareSessions(sessionA, sessionB);
            MessageBox.Show(this, report, "Session Performance Comparison", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_bench.SessionHistory.Count == 0)
            {
                MessageBox.Show(this, "No recorded benchmark sessions to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = "Benchmark_Session.csv" };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, _bench.SessionHistory.Last().ToCsv());
                MessageBox.Show(this, "Benchmark session data exported successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _telemetryTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
