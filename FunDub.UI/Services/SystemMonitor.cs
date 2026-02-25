using System.Diagnostics;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Polls system performance metrics (CPU, RAM, GPU) on a background timer.
    /// Supports NVIDIA (nvidia-smi), AMD, and Intel GPU monitoring.
    /// </summary>
    public class SystemMonitor : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private PerformanceCounter? _cpuCounter;
        private bool _disposed;
        private GpuVendor _detectedGpu = GpuVendor.Unknown;

        public event Action<SystemMetrics>? OnMetricsUpdated;

        private enum GpuVendor { Unknown, Nvidia, Amd, Intel }

        public SystemMonitor(int intervalMs = 1000)
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += (_, _) => Poll();

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // Prime it
            }
            catch { _cpuCounter = null; }

            DetectGpuVendor();
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void DetectGpuVendor()
        {
            // Try NVIDIA first
            if (TryRunProcess("nvidia-smi", "--version"))
            {
                _detectedGpu = GpuVendor.Nvidia;
                return;
            }

            // Check for AMD or Intel via WMI description
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Name FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString()?.ToLower() ?? "";
                    if (name.Contains("radeon") || name.Contains("amd"))
                    {
                        _detectedGpu = GpuVendor.Amd;
                        return;
                    }
                    if (name.Contains("intel") && (name.Contains("arc") || name.Contains("iris") || name.Contains("uhd")))
                    {
                        _detectedGpu = GpuVendor.Intel;
                        return;
                    }
                }
            }
            catch { }
        }

        private void Poll()
        {
            try
            {
                var metrics = new SystemMetrics();

                // CPU
                if (_cpuCounter != null)
                {
                    try { metrics.CpuPercent = (int)_cpuCounter.NextValue(); }
                    catch { }
                }

                // RAM
                try
                {
                    var proc = Process.GetCurrentProcess();
                    metrics.RamMb = (int)(proc.WorkingSet64 / (1024 * 1024));
                }
                catch { }

                // GPU
                metrics.GpuPercent = _detectedGpu switch
                {
                    GpuVendor.Nvidia => GetNvidiaGpuUsage(),
                    GpuVendor.Amd => GetAmdGpuUsage(),
                    GpuVendor.Intel => GetIntelGpuUsage(),
                    _ => 0
                };

                OnMetricsUpdated?.Invoke(metrics);
            }
            catch { }
        }

        // ── NVIDIA ──────────────────────────────────────────────────
        private static int GetNvidiaGpuUsage()
        {
            return QueryProcess("nvidia-smi",
                "--query-gpu=utilization.gpu --format=csv,noheader,nounits");
        }

        // ── AMD ─────────────────────────────────────────────────────
        // Uses the GPU Engine performance counter available on Windows 10+
        private static int GetAmdGpuUsage()
        {
            return GetGpuEngineUsage();
        }

        // ── Intel ───────────────────────────────────────────────────
        private static int GetIntelGpuUsage()
        {
            return GetGpuEngineUsage();
        }

        /// <summary>
        /// Uses Windows 10+ "GPU Engine" performance counters.
        /// Works for AMD, Intel, and NVIDIA GPUs.
        /// </summary>
        private static int GetGpuEngineUsage()
        {
            try
            {
                // Windows 10 1709+ exposes GPU usage via performance counters
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();

                double maxUtilization = 0;
                foreach (string instance in instanceNames)
                {
                    // Filter for 3D engine (the one used for video encoding)
                    if (!instance.Contains("engtype_3D") && !instance.Contains("engtype_VideoDecode")
                        && !instance.Contains("engtype_VideoEncode"))
                        continue;

                    try
                    {
                        using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        double val = counter.NextValue();
                        if (val > maxUtilization) maxUtilization = val;
                    }
                    catch { }
                }

                return (int)Math.Min(maxUtilization, 100);
            }
            catch
            {
                return 0;
            }
        }

        private static int QueryProcess(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return 0;

                string output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(1000);

                if (int.TryParse(output.Split('\n')[0].Trim(), out int pct))
                    return pct;
            }
            catch { }

            return 0;
        }

        private static bool TryRunProcess(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(2000);
                return proc?.ExitCode == 0;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
            _cpuCounter?.Dispose();
        }
    }

    public class SystemMetrics
    {
        public int CpuPercent { get; set; }
        public int GpuPercent { get; set; }
        public int RamMb { get; set; }
    }
}
