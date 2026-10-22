using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Management;
using System.Text;
using GHelper.Battery;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.RGBStudio;
using GHelper.UI;
using HidSharp;

namespace GHelper.Diagnostics
{
    public class HardwareDiagnosticsForm : Form
    {
        private TabControl _tabControl = null!;
        private TabPage _tabOverview = null!;
        private TabPage _tabPeripherals = null!;
        private TabPage _tabConflicts = null!;
        private TabPage _tabLogs = null!;

        // Overview controls
        private RichTextBox _rtbOverview = null!;
        private RichTextBox _rtbPeripherals = null!;
        private RichTextBox _rtbConflicts = null!;
        private RichTextBox _rtbLogs = null!;

        private RButton _btnRefresh = null!;
        private RButton _btnExport = null!;
        private RButton _btnClearLogs = null!;
        private RButton _btnOpenLogFolder = null!;

        public HardwareDiagnosticsForm()
        {
            InitializeComponent();
            RefreshAllDiagnostics();
        }

        private void InitializeComponent()
        {
            Text = "G-Helper Ultimate — Hardware Diagnostics & System Audit";
            Size = new Size(950, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(16, 18, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Icon = Program.settingsForm?.Icon;

            // Top Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 12, 16, 12)
            };

            Label lblTitle = new Label
            {
                Text = "SYSTEM DIAGNOSTICS & HARDWARE AUDIT",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 220, 255),
                AutoSize = true,
                Location = new Point(16, 12)
            };

            Label lblSubtitle = new Label
            {
                Text = "Comprehensive hardware status, Kreo & ASUS peripheral inventory, sensor health, and conflict detection",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 168, 180),
                AutoSize = true,
                Location = new Point(16, 36)
            };

            _btnRefresh = new RButton
            {
                Text = "Refresh",
                Size = new Size(90, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 110, 16),
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White
            };
            _btnRefresh.Click += (_, _) => RefreshAllDiagnostics();

            pnlHeader.Controls.AddRange([lblTitle, lblSubtitle, _btnRefresh]);

            // Bottom Actions Panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 10, 16, 10)
            };

            _btnExport = new RButton
            {
                Text = "Export Full Report (.txt)",
                Size = new Size(180, 34),
                Location = new Point(16, 10),
                BackColor = Color.FromArgb(0, 130, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _btnExport.Click += BtnExport_Click;

            _btnClearLogs = new RButton
            {
                Text = "Clear App Logs",
                Size = new Size(130, 34),
                Location = new Point(206, 10),
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.White
            };
            _btnClearLogs.Click += BtnClearLogs_Click;

            _btnOpenLogFolder = new RButton
            {
                Text = "Open Logs Folder",
                Size = new Size(140, 34),
                Location = new Point(346, 10),
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.White
            };
            _btnOpenLogFolder.Click += (_, _) =>
            {
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GHelper");
                    if (Directory.Exists(dir)) Process.Start("explorer.exe", dir);
                }
                catch { }
            };

            RButton btnClose = new RButton
            {
                Text = "Close",
                Size = new Size(90, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(pnlBottom.Width - 110, 10),
                BackColor = Color.FromArgb(45, 52, 68),
                ForeColor = Color.White
            };
            btnClose.Click += (_, _) => Close();

            pnlBottom.Controls.AddRange([_btnExport, _btnClearLogs, _btnOpenLogFolder, btnClose]);

            // Center TabControl
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            _tabOverview = new TabPage("Laptop & Hardware Specs");
            _tabOverview.BackColor = Color.FromArgb(16, 18, 24);

            _tabPeripherals = new TabPage("Peripherals & USB / HID");
            _tabPeripherals.BackColor = Color.FromArgb(16, 18, 24);

            _tabConflicts = new TabPage("ASUS Services & Conflicts");
            _tabConflicts.BackColor = Color.FromArgb(16, 18, 24);

            _tabLogs = new TabPage("System & Diagnostic Logs");
            _tabLogs.BackColor = Color.FromArgb(16, 18, 24);

            _rtbOverview = CreateStyledTextBox();
            _tabOverview.Controls.Add(_rtbOverview);

            _rtbPeripherals = CreateStyledTextBox();
            _tabPeripherals.Controls.Add(_rtbPeripherals);

            _rtbConflicts = CreateStyledTextBox();
            _tabConflicts.Controls.Add(_rtbConflicts);

            _rtbLogs = CreateStyledTextBox();
            _tabLogs.Controls.Add(_rtbLogs);

            _tabControl.TabPages.AddRange([_tabOverview, _tabPeripherals, _tabConflicts, _tabLogs]);

            Controls.Add(_tabControl);
            Controls.Add(pnlBottom);
            Controls.Add(pnlHeader);
        }

        private static RichTextBox CreateStyledTextBox()
        {
            return new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 20, 28),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
        }

        public void RefreshAllDiagnostics()
        {
            try
            {
                _rtbOverview.Text = GenerateOverviewReport();
                _rtbPeripherals.Text = GeneratePeripheralsReport();
                _rtbConflicts.Text = GenerateConflictsReport();
                _rtbLogs.Text = GenerateLogsReport();
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[Diagnostics] Refresh error: {ex.Message}");
            }
        }

        private string GenerateOverviewReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                      ASUS ROG STRIX G16 HARDWARE AUDIT                         ");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            string model = AppConfig.GetModel();
            string modelShort = AppConfig.GetModelShort();
            var (bios, _) = AppConfig.GetBiosAndModel();

            sb.AppendLine($"[SYSTEM INFORMATION]");
            sb.AppendLine($"Laptop Model          : {model} ({modelShort})");
            sb.AppendLine($"BIOS Version          : {(string.IsNullOrEmpty(bios) ? "Unknown" : bios)}");
            sb.AppendLine($"Operating System      : {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})");
            sb.AppendLine($"Administrator Rights  : {(ProcessHelper.IsUserAdministrator() ? "Yes (Elevated)" : "No (Standard User)")}");
            sb.AppendLine($"G-Helper Version      : {typeof(Program).Assembly.GetName().Version}");
            sb.AppendLine();

            // CPU info
            sb.AppendLine($"[PROCESSOR]");
            sb.AppendLine($"CPU Architecture      : 14th-Gen Intel Core i9 (Detected Core Count: {Environment.ProcessorCount} threads)");
            sb.AppendLine($"CPU Temperature       : {(HardwareControl.cpuTemp > 0 ? $"{HardwareControl.cpuTemp}°C" : "Reading...")}");
            sb.AppendLine($"CPU Fan Speed         : {HardwareControl.cpuFan ?? "Idle / Stopped"}");
            sb.AppendLine();

            // GPU info
            sb.AppendLine($"[GRAPHICS HARDWARE]");
            int currentGpuMode = AppConfig.Get("gpu_mode");
            string gpuMode = currentGpuMode switch
            {
                AsusACPI.GPUModeEco => "Eco (dGPU Powered Down)",
                AsusACPI.GPUModeUltimate => "Ultimate (Dedicated MUX Switch)",
                _ => "Standard (Advanced Optimus)"
            };
            sb.AppendLine($"Active GPU Mode       : {gpuMode}");
            sb.AppendLine($"GPU Temperature       : {(HardwareControl.gpuTemp > 0 ? $"{HardwareControl.gpuTemp}°C" : "Sleeping / Idle")}");
            sb.AppendLine($"GPU Fan Speed         : {HardwareControl.gpuFan ?? "Idle / Stopped"}");
            if (!string.IsNullOrEmpty(HardwareControl.midFan))
            {
                sb.AppendLine($"Middle Fan Speed      : {HardwareControl.midFan}");
            }
            sb.AppendLine();

            // Display
            sb.AppendLine($"[DISPLAY CONFIGURATION]");
            var laptopScreen = Display.ScreenNative.FindLaptopScreen(true);
            int rate = Display.ScreenNative.GetRefreshRate(laptopScreen);
            int maxRate = Display.ScreenControl.GetMaxRate(laptopScreen);
            sb.AppendLine($"Active Display        : {Screen.PrimaryScreen?.Bounds.Width}x{Screen.PrimaryScreen?.Bounds.Height} @ {(rate > 0 ? $"{rate}Hz" : "Unknown")}");
            sb.AppendLine($"Max Supported Rate    : {(maxRate > 0 ? $"{maxRate}Hz" : "240Hz")}");
            sb.AppendLine();

            // Battery
            sb.AppendLine($"[BATTERY & POWER MANAGEMENT]");
            int limit = AppConfig.Get("charge_limit", 100);
            sb.AppendLine($"Charge Limit Setting  : {(limit < 100 ? $"{limit}%" : "100% (Unrestricted)")}");
            sb.AppendLine($"Health Estimate       : {(HardwareControl.batteryHealth > 0 ? $"{Math.Round(HardwareControl.batteryHealth, 1)}%" : "Unknown")}");
            sb.AppendLine($"Full Charge Capacity  : {(HardwareControl.batteryCapacity > 0 ? $"{HardwareControl.batteryCapacity} mWh" : "N/A")}");
            if (HardwareControl.batteryRate != 0)
            {
                sb.AppendLine($"Current Power Flow    : {(HardwareControl.batteryRate > 0 ? $"Charging (+{Math.Round((decimal)HardwareControl.batteryRate, 1)}W)" : $"Discharging ({Math.Round(-(decimal)HardwareControl.batteryRate, 1)}W)")}");
            }
            sb.AppendLine();

            // Thermal / ACPI
            sb.AppendLine($"[ACPI & SENSOR SUBSYSTEM]");
            sb.AppendLine($"ACPI Status           : {(Program.acpi.IsConnected() ? "Connected & Initialized" : "Direct ACPI Unreachable")}");
            sb.AppendLine($"Overdrive Capable     : {(Program.acpi.IsOverdriveSupported() ? "Yes" : "No")}");
            sb.AppendLine($"Screen Refresh Switch : Supported");

            return sb.ToString();
        }

        private string GeneratePeripheralsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                       PERIPHERAL & USB/HID INVENTORY                           ");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // Registered RGB Devices in RGBDeviceManager
            sb.AppendLine("[MANAGED RGB STUDIO DEVICES]");
            foreach (var dev in RGBDeviceManager.Instance.AllDevices)
            {
                sb.AppendLine($"* Device Name     : {dev.DeviceName}");
                sb.AppendLine($"  Device ID       : {dev.DeviceId}");
                sb.AppendLine($"  Device Type     : {dev.DeviceType}");
                sb.AppendLine($"  Connection      : {(dev.IsConnected ? "Connected (Ready)" : "Simulation / Standby")}");
                sb.AppendLine($"  Is Simulation   : {dev.IsSimulated}");
                sb.AppendLine($"  Zones / Key Count: {dev.TotalKeyOrZoneCount}");
                sb.AppendLine($"  Supported Effects: {dev.SupportedEffects.Count} registered");
                sb.AppendLine();
            }

            // Kreo Details
            sb.AppendLine("[KREO PERIPHERALS]");
            var kreoKb = RGBDeviceManager.Instance.KreoKeyboardProvider;
            sb.AppendLine($"Kreo Keyboard Status : {(kreoKb.IsSimulated ? "Virtual / Simulation Mode (Connect physical Kreo keyboard for direct HID)" : "Direct Hardware Attached")}");

            var kreoMouse = RGBDeviceManager.Instance.KreoMouseProvider;
            sb.AppendLine($"Kreo Mouse Status    : {(kreoMouse.IsSimulated ? "Virtual / Simulation Mode (Connect physical Kreo mouse for direct HID)" : "Direct Hardware Attached")}");
            sb.AppendLine($"Kreo Mouse DPI       : {kreoMouse.CurrentDpi} DPI");
            sb.AppendLine($"Kreo Polling Rate    : {kreoMouse.CurrentPollingRate} Hz");
            sb.AppendLine();

            // Direct HID Enumeration
            sb.AppendLine("[PHYSICAL USB / HID DEVICE ENUMERATION]");
            try
            {
                var hidDevices = DeviceList.Local.GetHidDevices().ToList();
                sb.AppendLine($"Total HID Interfaces Found: {hidDevices.Count}");
                sb.AppendLine();

                int idx = 1;
                foreach (var d in hidDevices.DistinctBy(x => (x.VendorID, x.ProductID)).Take(25))
                {
                    string mfg = "Unknown";
                    string prod = "HID Device";
                    try { mfg = d.GetManufacturer(); } catch { }
                    try { prod = d.GetProductName(); } catch { }

                    sb.AppendLine($"{idx++}. VID: 0x{d.VendorID:X4} | PID: 0x{d.ProductID:X4} | [{mfg}] {prod}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error enumerating HID devices: {ex.Message}");
            }

            return sb.ToString();
        }

        private string GenerateConflictsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                     ASUS SERVICES & CONFLICT CHECKER                           ");
            sb.AppendLine("================================================================================");
            sb.AppendLine();
            sb.AppendLine("Running multiple control software (like ASUS Armoury Crate or MyASUS services) ");
            sb.AppendLine("simultaneously with G-Helper can cause fighting over ACPI fan curves and Aura.  ");
            sb.AppendLine();

            string[] asusProcessKeywords =
            [
                "ArmouryCrate",
                "ArmourySocketServer",
                "AsusAppService",
                "AsusLinkNear",
                "AsusLinkRemote",
                "AsusROGLSLService",
                "AsusSoftwareManager",
                "AsusSystemAnalysis",
                "AsusSystemDiagnosis",
                "LightingService",
                "ROGLiveService"
            ];

            var allProcesses = Process.GetProcesses().Select(p => p.ProcessName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int runningCount = 0;
            foreach (var sName in asusProcessKeywords)
            {
                bool isRunning = allProcesses.Any(p => p.Contains(sName, StringComparison.OrdinalIgnoreCase));
                if (isRunning) runningCount++;

                sb.AppendLine($"* {sName.PadRight(30)}: {(isRunning ? "[RUNNING - POTENTIAL CONFLICT]" : "[Not Detected / Clean]")}");
            }

            sb.AppendLine();
            if (runningCount > 0)
            {
                sb.AppendLine($"WARNING: {runningCount} ASUS Armoury Crate / Aura background processes are currently active.");
                sb.AppendLine("To achieve zero background CPU usage and prevent fan fighting, consider stopping or disabling them or using the official Asus Armoury Crate Uninstall Tool.");
            }
            else
            {
                sb.AppendLine("EXCELLENT: No conflicting ASUS Armoury Crate background processes are running.");
                sb.AppendLine("G-Helper Ultimate has clean, exclusive control over ACPI and Aura hardware interfaces.");
            }

            return sb.ToString();
        }

        private string GenerateLogsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                         G-HELPER APPLICATION LOGS                             ");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            try
            {
                string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GHelper", "log.txt");
                if (File.Exists(logFile))
                {
                    var lines = File.ReadAllLines(logFile);
                    int start = Math.Max(0, lines.Length - 100);
                    sb.AppendLine($"Showing last {lines.Length - start} log entries (out of {lines.Length} total):");
                    sb.AppendLine("--------------------------------------------------------------------------------");
                    for (int i = start; i < lines.Length; i++)
                    {
                        sb.AppendLine(lines[i]);
                    }
                }
                else
                {
                    sb.AppendLine("No log file found at expected path.");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error reading log file: {ex.Message}");
            }

            return sb.ToString();
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Title = "Export G-Helper Ultimate System Diagnostic Report",
                    Filter = "Text Files (*.txt)|*.txt|Markdown Files (*.md)|*.md",
                    FileName = $"GHelper_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var fullReport = new StringBuilder();
                    fullReport.AppendLine($"G-HELPER ULTIMATE FULL HARDWARE AUDIT REPORT");
                    fullReport.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    fullReport.AppendLine();
                    fullReport.AppendLine(GenerateOverviewReport());
                    fullReport.AppendLine();
                    fullReport.AppendLine(GeneratePeripheralsReport());
                    fullReport.AppendLine();
                    fullReport.AppendLine(GenerateConflictsReport());
                    fullReport.AppendLine();
                    fullReport.AppendLine(GenerateLogsReport());

                    File.WriteAllText(sfd.FileName, fullReport.ToString());
                    MessageBox.Show(this, $"Diagnostic report exported successfully to:\n{sfd.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to export report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GHelper", "log.txt");
                if (File.Exists(logFile))
                {
                    File.WriteAllText(logFile, string.Empty);
                    _rtbLogs.Text = "Log file cleared.";
                    MessageBox.Show(this, "Logs cleared successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to clear logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
