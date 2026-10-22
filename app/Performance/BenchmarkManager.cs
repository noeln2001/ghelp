using System.Diagnostics;
using GHelper.Helpers;
using GHelper.Mode;

namespace GHelper.Performance
{
    public class BenchmarkManager
    {
        private static readonly Lazy<BenchmarkManager> _instance = new(() => new BenchmarkManager());
        public static BenchmarkManager Instance => _instance.Value;

        public bool IsRecording { get; private set; } = false;
        public BenchmarkSession? ActiveSession { get; private set; }
        public List<BenchmarkSession> SessionHistory { get; } = [];

        private System.Windows.Forms.Timer? _samplingTimer;
        private Stopwatch _stopwatch = new();
        private long _lastFrameTimestamp = 0;

        public event EventHandler? SampleRecorded;
        public event EventHandler? SessionCompleted;

        public BenchmarkManager()
        {
            _samplingTimer = new System.Windows.Forms.Timer { Interval = 100 }; // 10 Hz sampling
            _samplingTimer.Tick += SamplingTimer_Tick;
        }

        public void StartBenchmark(string gameTitle, string profileName)
        {
            ActiveSession = new BenchmarkSession
            {
                GameTitle = gameTitle,
                PerformanceProfile = profileName,
                StartTime = DateTime.Now
            };

            IsRecording = true;
            _stopwatch.Restart();
            _lastFrameTimestamp = Stopwatch.GetTimestamp();
            _samplingTimer?.Start();
            Logger.WriteLine($"[Benchmark] Started session for '{gameTitle}' in profile '{profileName}'");
        }

        public BenchmarkSession? StopBenchmark()
        {
            if (!IsRecording || ActiveSession == null) return null;

            _samplingTimer?.Stop();
            _stopwatch.Stop();
            IsRecording = false;

            ActiveSession.DurationSeconds = _stopwatch.Elapsed.TotalSeconds;
            ActiveSession.CalculateStatistics();

            SessionHistory.Add(ActiveSession);
            Logger.WriteLine($"[Benchmark] Session completed: {ActiveSession.Samples.Count} samples, Avg FPS: {ActiveSession.AverageFps}, 1% Low: {ActiveSession.OnePercentLowFps}");

            SessionCompleted?.Invoke(this, EventArgs.Empty);
            return ActiveSession;
        }

        private void SamplingTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsRecording || ActiveSession == null) return;

            long currentTimestamp = Stopwatch.GetTimestamp();
            double deltaMs = (currentTimestamp - _lastFrameTimestamp) * 1000.0 / Stopwatch.Frequency;
            _lastFrameTimestamp = currentTimestamp;

            double fps = deltaMs > 0 ? Math.Clamp(1000.0 / deltaMs, 10.0, 500.0) : 60.0;

            float cpuTemp = HardwareControl.cpuTemp ?? 0;
            float gpuTemp = HardwareControl.gpuTemp ?? 0;
            float cpuUsage = HardwareControl.cpuUsage ?? 0;
            float gpuUsage = HardwareControl.gpuUsage ?? 0;
            float cpuWatts = HardwareControl.cpuPower ?? 0;
            float gpuWatts = HardwareControl.gpuPower ?? 0;
            float ramGb = (float)Math.Round((Environment.WorkingSet / (1024.0 * 1024.0 * 1024.0)), 2);

            var sample = new BenchmarkSample
            {
                TimestampSeconds = _stopwatch.Elapsed.TotalSeconds,
                Fps = fps,
                FrameTimeMs = deltaMs,
                CpuTemp = cpuTemp,
                GpuTemp = gpuTemp,
                CpuUsage = cpuUsage,
                GpuUsage = gpuUsage,
                CpuPowerWatts = cpuWatts,
                GpuPowerWatts = gpuWatts,
                RamUsageGb = ramGb
            };

            ActiveSession.Samples.Add(sample);
            SampleRecorded?.Invoke(this, EventArgs.Empty);
        }
    }
}
