using GHelper.Helpers;
using GHelper.Mode;
using GHelper.RGBStudio;
using GHelper.UI;

namespace GHelper.Games
{
    public class GameProfilesForm : RForm
    {
        private ListBox _listGames = default!;
        private TextBox _textTitle = default!;
        private TextBox _textExe = default!;
        private ComboBox _comboMode = default!;
        private ComboBox _comboRgb = default!;
        private ComboBox _comboHz = default!;
        private CheckBox _checkBooster = default!;
        private RButton _btnSave = default!;
        private RButton _btnScan = default!;
        private RButton _btnBrowse = default!;
        private RButton _btnDelete = default!;

        private List<DetectedGame> _detected = [];

        public GameProfilesForm()
        {
            InitializeComponent();
            InitTheme(true);
            RefreshGameList();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper — Smart Game Profiles";
            Size = new Size(840, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(740, 480);
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
                Text = "SMART GAME PROFILES & LAUNCHER INTEGRATION",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 200),
                AutoSize = true,
                Location = new Point(15, 16)
            };
            panelHeader.Controls.Add(labelTitle);
            Controls.Add(panelHeader);

            // Left: Games List
            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 340,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(20, 20, 28)
            };

            var lblList = new Label { Text = "DETECTED & CONFIGURED GAMES", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.DarkGray, Location = new Point(15, 15) };
            panelLeft.Controls.Add(lblList);

            _listGames = new ListBox
            {
                Location = new Point(15, 40),
                Width = 310,
                Height = 380,
                BackColor = Color.FromArgb(25, 25, 34),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            _listGames.SelectedIndexChanged += ListGames_SelectedIndexChanged;
            panelLeft.Controls.Add(_listGames);

            _btnScan = new RButton { Text = "Scan Launchers", Location = new Point(15, 430), Size = new Size(150, 34), Secondary = true };
            _btnScan.Click += (s, e) =>
            {
                _detected = GameDetectionProvider.ScanInstalledGames();
                RefreshGameList();
                MessageBox.Show(this, $"Scanned launchers: Found {_detected.Count} installed games!", "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _btnBrowse = new RButton { Text = "Add Custom Exe", Location = new Point(175, 430), Size = new Size(150, 34), Secondary = true };
            _btnBrowse.Click += BtnBrowse_Click;

            panelLeft.Controls.AddRange([_btnScan, _btnBrowse]);
            Controls.Add(panelLeft);

            // Right: Profile Settings Editor
            var panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(16, 16, 22)
            };
            BuildEditor(panelRight);
            Controls.Add(panelRight);
        }

        private void BuildEditor(Panel parent)
        {
            int y = 20;

            var lblTitle = new Label { Text = "Game Title:", Location = new Point(20, y), AutoSize = true, ForeColor = Color.LightGray };
            _textTitle = new RTextBox { Location = new Point(20, y + 22), Width = 400 };
            parent.Controls.AddRange([lblTitle, _textTitle]);
            y += 60;

            var lblExe = new Label { Text = "Executable Name (without .exe):", Location = new Point(20, y), AutoSize = true, ForeColor = Color.LightGray };
            _textExe = new RTextBox { Location = new Point(20, y + 22), Width = 400 };
            parent.Controls.AddRange([lblExe, _textExe]);
            y += 60;

            var lblMode = new Label { Text = "Performance Mode:", Location = new Point(20, y), AutoSize = true, ForeColor = Color.LightGray };
            _comboMode = new RComboBox { Location = new Point(20, y + 22), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            _comboMode.Items.AddRange(["Silent Mode (Quiet & Cool)", "Balanced Mode (Optimal)", "Turbo Mode (Max Performance)"]);
            _comboMode.SelectedIndex = 2; // Turbo
            parent.Controls.AddRange([lblMode, _comboMode]);
            y += 60;

            var lblRgb = new Label { Text = "RGB Studio Theme:", Location = new Point(20, y), AutoSize = true, ForeColor = Color.LightGray };
            _comboRgb = new RComboBox { Location = new Point(20, y + 22), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var t in RGBThemeManager.BuiltInThemes) _comboRgb.Items.Add(t.Name);
            _comboRgb.SelectedIndex = 0;
            parent.Controls.AddRange([lblRgb, _comboRgb]);
            y += 60;

            var lblHz = new Label { Text = "Target Refresh Rate:", Location = new Point(20, y), AutoSize = true, ForeColor = Color.LightGray };
            _comboHz = new RComboBox { Location = new Point(20, y + 22), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            _comboHz.Items.AddRange(["240 Hz (Max Smoothness)", "120 Hz", "60 Hz (Power Saving)"]);
            _comboHz.SelectedIndex = 0;
            parent.Controls.AddRange([lblHz, _comboHz]);
            y += 60;

            _checkBooster = new CheckBox
            {
                Text = "Enable Game Booster (Auto-kill background tasks & clean RAM on start)",
                Location = new Point(20, y),
                Width = 420,
                Checked = true,
                ForeColor = Color.FromArgb(0, 255, 200),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            parent.Controls.Add(_checkBooster);
            y += 45;

            _btnSave = new RButton { Text = "Save Profile", Location = new Point(20, y), Size = new Size(160, 36), Activated = true };
            _btnSave.Click += BtnSave_Click;

            _btnDelete = new RButton { Text = "Remove", Location = new Point(190, y), Size = new Size(120, 36), Secondary = true };
            _btnDelete.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(_textExe.Text))
                {
                    SmartGameProfileManager.RemoveProfile(_textExe.Text.Trim().ToLowerInvariant());
                    RefreshGameList();
                }
            };

            parent.Controls.AddRange([_btnSave, _btnDelete]);
        }

        private void RefreshGameList()
        {
            _listGames.Items.Clear();

            // Configured profiles first
            foreach (var kvp in SmartGameProfileManager.Profiles)
            {
                _listGames.Items.Add($"★ {kvp.Value.Title} ({kvp.Key})");
            }

            // Detected games
            foreach (var g in _detected)
            {
                if (!SmartGameProfileManager.Profiles.ContainsKey(g.ExecutableName))
                {
                    _listGames.Items.Add($"[{g.Launcher}] {g.Title}");
                }
            }
        }

        private void ListGames_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_listGames.SelectedIndex < 0) return;
            string selected = _listGames.SelectedItem?.ToString() ?? "";

            // Check if it's an existing profile
            foreach (var p in SmartGameProfileManager.Profiles.Values)
            {
                if (selected.Contains(p.Title) || selected.Contains(p.ExecutableName))
                {
                    _textTitle.Text = p.Title;
                    _textExe.Text = p.ExecutableName;
                    _comboMode.SelectedIndex = p.PerformanceMode == AsusACPI.PerformanceSilent ? 0 : p.PerformanceMode == AsusACPI.PerformanceBalanced ? 1 : 2;
                    _comboRgb.SelectedItem = p.RgbThemeName;
                    _comboHz.SelectedIndex = p.TargetRefreshRate == 60 ? 2 : p.TargetRefreshRate == 120 ? 1 : 0;
                    _checkBooster.Checked = p.EnableGameBooster;
                    return;
                }
            }

            // Check if it's a detected game
            foreach (var g in _detected)
            {
                if (selected.Contains(g.Title))
                {
                    _textTitle.Text = g.Title;
                    _textExe.Text = g.ExecutableName;
                    _comboMode.SelectedIndex = 2; // Turbo
                    _comboHz.SelectedIndex = 0; // 240Hz
                    _checkBooster.Checked = true;
                    return;
                }
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "Executable Files (*.exe)|*.exe" };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string title = Path.GetFileNameWithoutExtension(ofd.FileName);
                string exeName = title.ToLowerInvariant();
                _textTitle.Text = title;
                _textExe.Text = exeName;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_textExe.Text))
            {
                MessageBox.Show(this, "Please specify an executable name for the game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int mode = _comboMode.SelectedIndex == 0 ? AsusACPI.PerformanceSilent : _comboMode.SelectedIndex == 1 ? AsusACPI.PerformanceBalanced : AsusACPI.PerformanceTurbo;
            int hz = _comboHz.SelectedIndex == 2 ? 60 : _comboHz.SelectedIndex == 1 ? 120 : 240;

            var profile = new SmartGameProfile
            {
                Title = _textTitle.Text.Trim(),
                ExecutableName = _textExe.Text.Trim().ToLowerInvariant(),
                PerformanceMode = mode,
                RgbThemeName = _comboRgb.SelectedItem?.ToString() ?? "Cyberpunk",
                TargetRefreshRate = hz,
                EnableGameBooster = _checkBooster.Checked
            };

            SmartGameProfileManager.SetProfile(profile);
            RefreshGameList();
            MessageBox.Show(this, $"Profile for '{profile.Title}' saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
