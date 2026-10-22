using System.Diagnostics;
using GHelper.Helpers;
using GHelper.UI;

namespace GHelper.SpatialAudio
{
    public class AudioStudioForm : Form
    {
        private ComboBox _comboPlaybackDevices = null!;
        private ComboBox _comboRecordingDevices = null!;
        private Label _lblFormatInfo = null!;
        private TrackBar _trackVolume = null!;
        private Label _lblVolumeVal = null!;
        private RButton _btnMute = null!;
        private RButton _btnMicMute = null!;
        private List<RButton> _codecModeButtons = [];
        private List<RButton> _eqPresetButtons = [];

        public AudioStudioForm()
        {
            InitializeComponent();
            RefreshAudioStatus();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper Ultimate — Audio Codecs & Spatial Sound Studio";
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
                Height = 75,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "AUDIO CODECS & SPATIAL SOUND STUDIO",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                AutoSize = true,
                Location = new Point(16, 10)
            };

            Label lblSubtitle = new Label
            {
                Text = "Dolby Digital 5.1, DTS:X Ultra, 2.0 Pure Stereo Studio, 7.1 Virtual Surround, and Gaming Audio Presets",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 168, 180),
                AutoSize = true,
                Location = new Point(16, 36)
            };

            RButton btnSoundCpl = new RButton
            {
                Text = "Sound Panel",
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 240, 16),
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White
            };
            btnSoundCpl.Click += (_, _) => AudioCodecManager.OpenSoundControlPanel();

            RButton btnMixer = new RButton
            {
                Text = "Volume Mixer",
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 120, 16),
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White
            };
            btnMixer.Click += (_, _) => AudioCodecManager.OpenVolumeMixer();

            pnlHeader.Controls.AddRange([lblTitle, lblSubtitle, btnSoundCpl, btnMixer]);

            // Bottom Actions Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            RButton btnDolbyApp = new RButton
            {
                Text = "Open Dolby Access",
                Size = new Size(160, 34),
                Location = new Point(16, 10),
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.White
            };
            btnDolbyApp.Click += (_, _) => AudioCodecManager.OpenDolbyAccess();

            RButton btnDtsApp = new RButton
            {
                Text = "Open DTS:X App",
                Size = new Size(140, 34),
                Location = new Point(186, 10),
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.White
            };
            btnDtsApp.Click += (_, _) => AudioCodecManager.OpenDtsUnbound();

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

            pnlBottom.Controls.AddRange([btnDolbyApp, btnDtsApp, btnClose]);

            // Center Content Panel (Scrollable)
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16)
            };

            int curY = 10;

            // SECTION 1: Active Output & Format
            Label lblDeviceSection = new Label
            {
                Text = "DEFAULT PLAYBACK & RECORDING DEVICES",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblDeviceSection);
            curY += 28;

            Panel pnlDevRow = new Panel { Location = new Point(0, curY), Size = new Size(880, 46) };
            Label lblPlay = new Label { Text = "Playback Output:", Location = new Point(0, 8), AutoSize = true };
            _comboPlaybackDevices = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(125, 4),
                Size = new Size(320, 28),
                BackColor = Color.FromArgb(28, 32, 44),
                ForeColor = Color.White
            };

            Label lblRec = new Label { Text = "Microphone Input:", Location = new Point(470, 8), AutoSize = true };
            _comboRecordingDevices = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(595, 4),
                Size = new Size(280, 28),
                BackColor = Color.FromArgb(28, 32, 44),
                ForeColor = Color.White
            };

            pnlDevRow.Controls.AddRange([lblPlay, _comboPlaybackDevices, lblRec, _comboRecordingDevices]);
            pnlContent.Controls.Add(pnlDevRow);
            curY += 50;

            // Format Info Badge
            _lblFormatInfo = new Label
            {
                Text = "Format: 2 Channels Stereo | 24-bit, 48000 Hz (Studio Quality)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(160, 175, 195),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(_lblFormatInfo);
            curY += 35;

            // SECTION 2: Audio Codec Modes (Dolby 5.1, DTS, 2.0 Stereo, 7.1)
            Label lblCodecSection = new Label
            {
                Text = "AUDIO CODEC & SPATIAL SOUND MODE",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblCodecSection);
            curY += 28;

            Panel pnlCodecs = new Panel { Location = new Point(0, curY), Size = new Size(880, 90) };
            (string Name, string Sub, AudioCodecMode Mode, Color Accent)[] codecCards =
            [
                ("2.0 Stereo", "Studio Lossless (Pure)", AudioCodecMode.Stereo20, Color.FromArgb(0, 160, 220)),
                ("Dolby Digital 5.1", "Dolby Atmos Surround", AudioCodecMode.DolbySurround51, Color.FromArgb(210, 40, 80)),
                ("DTS:X Ultra", "DTS Headphone:X Spatial", AudioCodecMode.DtsHeadphoneX, Color.FromArgb(220, 130, 0)),
                ("Windows Sonic", "Spatial Sound 3D", AudioCodecMode.WindowsSonic, Color.FromArgb(90, 80, 210)),
                ("7.1 Surround", "8-Channel Virtualized", AudioCodecMode.Surround71, Color.FromArgb(30, 160, 90))
            ];

            int cX = 0;
            int cardW = 168;
            foreach (var (cName, cSub, cMode, cAccent) in codecCards)
            {
                RButton btnC = new RButton
                {
                    Text = $"{cName}\n{cSub}",
                    Size = new Size(cardW, 76),
                    Location = new Point(cX, 0),
                    BackColor = Color.FromArgb(26, 30, 42),
                    BorderColor = cMode == AudioCodecManager.CurrentCodecMode ? cAccent : Color.Transparent,
                    BorderRadius = 6,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                };
                btnC.Click += (s, e) =>
                {
                    AudioCodecManager.SetCodecMode(cMode);
                    UpdateCodecButtonSelection(btnC, cAccent);
                    Program.toast.RunToast($"Audio Mode: {cName}", ToastIcon.Charger);
                };
                _codecModeButtons.Add(btnC);
                pnlCodecs.Controls.Add(btnC);
                cX += cardW + 10;
            }

            pnlContent.Controls.Add(pnlCodecs);
            curY += 105;

            // SECTION 3: Sound Profiles & EQ Presets
            Label lblEqSection = new Label
            {
                Text = "GAMING & AUDIO TUNING PRESETS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblEqSection);
            curY += 28;

            Panel pnlEq = new Panel { Location = new Point(0, curY), Size = new Size(880, 75) };
            (string EqName, string EqDesc, AudioEqPreset Preset, Color Color)[] eqPresets =
            [
                ("🎯 FPS Footsteps", "Competitive Gaming Cues", AudioEqPreset.FpsCompetitive, Color.FromArgb(200, 30, 60)),
                ("🎬 Movie Cinema", "Deep Bass & Dolby 5.1", AudioEqPreset.CinematicMovie, Color.FromArgb(180, 50, 150)),
                ("🎵 Hi-Fi Lossless", "Flat Pure Acoustic 2.0", AudioEqPreset.FlatHiFi, Color.FromArgb(0, 140, 200)),
                ("🎙️ Voice & Discord", "Speech Clarity & Noise Gate", AudioEqPreset.VoiceClarity, Color.FromArgb(40, 140, 80)),
                ("🌙 Night Mode", "Dynamic Range Compression", AudioEqPreset.NightMode, Color.FromArgb(100, 70, 150))
            ];

            int eX = 0;
            foreach (var (eqName, eqDesc, eqPreset, eqColor) in eqPresets)
            {
                RButton btnE = new RButton
                {
                    Text = $"{eqName}\n{eqDesc}",
                    Size = new Size(cardW, 64),
                    Location = new Point(eX, 0),
                    BackColor = Color.FromArgb(26, 30, 42),
                    BorderColor = eqPreset == AudioCodecManager.CurrentEqPreset ? eqColor : Color.Transparent,
                    BorderRadius = 6,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
                };
                btnE.Click += (s, e) =>
                {
                    AudioCodecManager.SetEqPreset(eqPreset);
                    UpdateEqButtonSelection(btnE, eqColor);
                    Program.toast.RunToast($"Preset: {eqName}", ToastIcon.Charger);
                };
                _eqPresetButtons.Add(btnE);
                pnlEq.Controls.Add(btnE);
                eX += cardW + 10;
            }

            pnlContent.Controls.Add(pnlEq);
            curY += 95;

            // SECTION 4: Volume & Microphone Quick Controls
            Label lblVolSection = new Label
            {
                Text = "MASTER VOLUME & MICROPHONE STATUS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(0, curY),
                AutoSize = true
            };
            pnlContent.Controls.Add(lblVolSection);
            curY += 28;

            Panel pnlVol = new Panel { Location = new Point(0, curY), Size = new Size(880, 50) };
            Label lblVol = new Label { Text = "Speaker Volume:", Location = new Point(0, 10), AutoSize = true };

            _trackVolume = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 75,
                TickFrequency = 10,
                Size = new Size(420, 32),
                Location = new Point(125, 4)
            };
            _trackVolume.Scroll += (_, _) =>
            {
                _lblVolumeVal.Text = $"{_trackVolume.Value}%";
                AudioCodecManager.SetMasterVolume(_trackVolume.Value / 100.0f);
            };

            _lblVolumeVal = new Label
            {
                Text = $"{_trackVolume.Value}%",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                Location = new Point(560, 10),
                AutoSize = true
            };

            _btnMute = new RButton
            {
                Text = "Mute Audio",
                Size = new Size(110, 32),
                Location = new Point(620, 6),
                BackColor = Color.FromArgb(42, 48, 64),
                ForeColor = Color.White
            };
            _btnMute.Click += (_, _) =>
            {
                bool muted = AudioCodecManager.ToggleMute();
                _btnMute.Text = muted ? "Unmute" : "Mute Audio";
                _btnMute.BackColor = muted ? Color.FromArgb(120, 30, 40) : Color.FromArgb(42, 48, 64);
            };

            _btnMicMute = new RButton
            {
                Text = "Mute Mic",
                Size = new Size(110, 32),
                Location = new Point(745, 6),
                BackColor = Color.FromArgb(42, 48, 64),
                ForeColor = Color.White
            };
            _btnMicMute.Click += (_, _) =>
            {
                bool muted = AudioCodecManager.ToggleMicMute();
                _btnMicMute.Text = muted ? "Unmute Mic" : "Mute Mic";
                _btnMicMute.BackColor = muted ? Color.FromArgb(120, 30, 40) : Color.FromArgb(42, 48, 64);
            };

            pnlVol.Controls.AddRange([lblVol, _trackVolume, _lblVolumeVal, _btnMute, _btnMicMute]);
            pnlContent.Controls.Add(pnlVol);

            Controls.Add(pnlContent);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        public void RefreshAudioStatus()
        {
            try
            {
                var playDevices = AudioCodecManager.GetPlaybackDevices();
                _comboPlaybackDevices.Items.Clear();
                foreach (var d in playDevices)
                {
                    _comboPlaybackDevices.Items.Add(d.Name + (d.IsDefault ? " [Default]" : ""));
                    if (d.IsDefault)
                    {
                        _comboPlaybackDevices.SelectedItem = _comboPlaybackDevices.Items[^1];
                        _lblFormatInfo.Text = $"Active Mix Format: {d.Channels} Channels ({(d.Channels == 6 ? "5.1 Surround" : (d.Channels == 8 ? "7.1 Surround" : "2.0 Stereo"))}) | {d.BitsPerSample}-bit, {d.SampleRate} Hz Lossless";
                        _trackVolume.Value = (int)(d.Volume * 100);
                        _lblVolumeVal.Text = $"{_trackVolume.Value}%";
                        _btnMute.Text = d.IsMuted ? "Unmute" : "Mute Audio";
                        _btnMute.BackColor = d.IsMuted ? Color.FromArgb(120, 30, 40) : Color.FromArgb(42, 48, 64);
                    }
                }

                var recDevices = AudioCodecManager.GetRecordingDevices();
                _comboRecordingDevices.Items.Clear();
                foreach (var d in recDevices)
                {
                    _comboRecordingDevices.Items.Add(d.Name + (d.IsDefault ? " [Default]" : ""));
                    if (d.IsDefault)
                    {
                        _comboRecordingDevices.SelectedItem = _comboRecordingDevices.Items[^1];
                        _btnMicMute.Text = d.IsMuted ? "Unmute Mic" : "Mute Mic";
                        _btnMicMute.BackColor = d.IsMuted ? Color.FromArgb(120, 30, 40) : Color.FromArgb(42, 48, 64);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioStudio] Refresh error: {ex.Message}");
            }
        }

        private void UpdateCodecButtonSelection(RButton selected, Color accent)
        {
            foreach (var b in _codecModeButtons)
            {
                b.BorderColor = (b == selected) ? accent : Color.Transparent;
            }
        }

        private void UpdateEqButtonSelection(RButton selected, Color accent)
        {
            foreach (var b in _eqPresetButtons)
            {
                b.BorderColor = (b == selected) ? accent : Color.Transparent;
            }
        }
    }
}
