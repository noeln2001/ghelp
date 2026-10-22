using System.Text;
using System.Text.Json;

namespace GHelper.Performance
{
    public class BenchmarkSample
    {
        public double TimestampSeconds { get; set; }
        public double Fps { get; set; }
        public double FrameTimeMs { get; set; }
        public float CpuTemp { get; set; }
        public float GpuTemp { get; set; }
        public float CpuUsage { get; set; }
        public float GpuUsage { get; set; }
        public float CpuPowerWatts { get; set; }
        public float GpuPowerWatts { get; set; }
        public float RamUsageGb { get; set; }
    }

    public class BenchmarkSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string GameTitle { get; set; } = "Game Session";
        public string PerformanceProfile { get; set; } = "Turbo";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public double DurationSeconds { get; set; }
        public List<BenchmarkSample> Samples { get; set; } = [];

        public double AverageFps { get; set; }
        public double MinFps { get; set; }
        public double MaxFps { get; set; }
        public double OnePercentLowFps { get; set; }
        public double ZeroPointOnePercentLowFps { get; set; }
        public double AverageFrameTimeMs { get; set; }
        public double FrameTimeStabilityScore { get; set; } // 0 - 100%

        public float AverageCpuTemp { get; set; }
        public float AverageGpuTemp { get; set; }
        public float MaxCpuTemp { get; set; }
        public float MaxGpuTemp { get; set; }
        public float AverageCpuPowerWatts { get; set; }
        public float AverageGpuPowerWatts { get; set; }

        public void CalculateStatistics()
        {
            if (Samples.Count == 0) return;

            var fpsValues = Samples.Select(s => s.Fps).OrderBy(f => f).ToList();
            AverageFps = Math.Round(fpsValues.Average(), 1);
            MinFps = Math.Round(fpsValues.First(), 1);
            MaxFps = Math.Round(fpsValues.Last(), 1);

            int onePercentIndex = Math.Max(0, (int)(fpsValues.Count * 0.01));
            OnePercentLowFps = Math.Round(fpsValues[onePercentIndex], 1);

            int zeroOnePercentIndex = Math.Max(0, (int)(fpsValues.Count * 0.001));
            ZeroPointOnePercentLowFps = Math.Round(fpsValues[zeroOnePercentIndex], 1);

            var frameTimes = Samples.Select(s => s.FrameTimeMs).ToList();
            AverageFrameTimeMs = Math.Round(frameTimes.Average(), 2);

            // Frame time stability: variance-based score
            double mean = frameTimes.Average();
            double variance = frameTimes.Select(val => Math.Pow(val - mean, 2)).Average();
            double stdDev = Math.Sqrt(variance);
            FrameTimeStabilityScore = Math.Round(Math.Clamp(100.0 - (stdDev * 5.0), 0.0, 100.0), 1);

            AverageCpuTemp = (float)Math.Round(Samples.Average(s => s.CpuTemp), 1);
            AverageGpuTemp = (float)Math.Round(Samples.Average(s => s.GpuTemp), 1);
            MaxCpuTemp = (float)Math.Round(Samples.Max(s => s.CpuTemp), 1);
            MaxGpuTemp = (float)Math.Round(Samples.Max(s => s.GpuTemp), 1);

            AverageCpuPowerWatts = (float)Math.Round(Samples.Average(s => s.CpuPowerWatts), 1);
            AverageGpuPowerWatts = (float)Math.Round(Samples.Average(s => s.GpuPowerWatts), 1);
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TimeSec,FPS,FrameTimeMs,CpuTempC,GpuTempC,CpuUsagePct,GpuUsagePct,CpuWatts,GpuWatts,RamGb");
            foreach (var s in Samples)
            {
                sb.AppendLine($"{s.TimestampSeconds:F2},{s.Fps:F1},{s.FrameTimeMs:F2},{s.CpuTemp:F1},{s.GpuTemp:F1},{s.CpuUsage:F1},{s.GpuUsage:F1},{s.CpuPowerWatts:F1},{s.GpuPowerWatts:F1},{s.RamUsageGb:F2}");
            }
            return sb.ToString();
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public static class BenchmarkComparison
    {
        public static string CompareSessions(BenchmarkSession a, BenchmarkSession b)
        {
            double fpsDelta = b.AverageFps - a.AverageFps;
            double fpsPct = a.AverageFps > 0 ? (fpsDelta / a.AverageFps) * 100 : 0;

            double lowDelta = b.OnePercentLowFps - a.OnePercentLowFps;
            double lowPct = a.OnePercentLowFps > 0 ? (lowDelta / a.OnePercentLowFps) * 100 : 0;

            float cpuTempDelta = b.AverageCpuTemp - a.AverageCpuTemp;
            float gpuTempDelta = b.AverageGpuTemp - a.AverageGpuTemp;

            return $"Comparison: [{a.PerformanceProfile}] vs [{b.PerformanceProfile}]\r\n" +
                   $"Average FPS: {a.AverageFps} -> {b.AverageFps} ({(fpsPct >= 0 ? "+" : "")}{fpsPct:F1}%)\r\n" +
                   $"1% Low FPS: {a.OnePercentLowFps} -> {b.OnePercentLowFps} ({(lowPct >= 0 ? "+" : "")}{lowPct:F1}%)\r\n" +
                   $"Avg CPU Temp: {a.AverageCpuTemp}°C -> {b.AverageCpuTemp}°C ({(cpuTempDelta >= 0 ? "+" : "")}{cpuTempDelta:F1}°C)\r\n" +
                   $"Avg GPU Temp: {a.AverageGpuTemp}°C -> {b.AverageGpuTemp}°C ({(gpuTempDelta >= 0 ? "+" : "")}{gpuTempDelta:F1}°C)\r\n" +
                   $"Stability Score: {a.FrameTimeStabilityScore}% vs {b.FrameTimeStabilityScore}%";
        }
    }
}
