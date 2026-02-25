using FunDub.UI.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Executes a list of <see cref="PipelineStep"/>s sequentially,
    /// reporting granular progress and supporting cancellation.
    /// </summary>
    public class PipelineRunner
    {
        private Process? _currentProcess;

        // ── Events ──────────────────────────────────────────────────
        /// <summary>Fires when a new step begins. (stepIndex, totalSteps, label)</summary>
        public event Action<int, int, string>? OnStepStarted;

        /// <summary>Fires with parsed progress data from FFmpeg stderr.</summary>
        public event Action<FFmpegProgress>? OnProgress;

        /// <summary>Fires with raw FFmpeg stderr lines (for logging / advanced monitor).</summary>
        public event Action<string>? OnRawOutput;

        /// <summary>Fires when the entire pipeline completes successfully.</summary>
        public event Action? OnCompleted;

        /// <summary>Fires when a step fails. (stepIndex, exitCode, lastOutput)</summary>
        public event Action<int, int, string>? OnError;

        // ── Run ─────────────────────────────────────────────────────
        public async Task RunAsync(List<PipelineStep> steps, CancellationToken ct = default)
        {
            var tempFiles = new List<string>();

            try
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var step = steps[i];
                    if (step.IsTempFile)
                        tempFiles.Add(step.OutputPath);

                    OnStepStarted?.Invoke(i, steps.Count, step.Label);

                    int exitCode = await RunFFmpegAsync(step.Arguments, ct);

                    if (exitCode != 0)
                    {
                        OnError?.Invoke(i, exitCode, _lastStderrLine);
                        return; // Stop pipeline on error
                    }
                }

                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Kill();
                throw;
            }
            finally
            {
                // Clean up temp files
                foreach (string path in tempFiles)
                {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch { /* best-effort cleanup */ }
                }
            }
        }

        // ── Cancel ──────────────────────────────────────────────────
        public void Kill()
        {
            try
            {
                if (_currentProcess is { HasExited: false })
                {
                    _currentProcess.Kill(entireProcessTree: true);
                }
            }
            catch { /* process may have already exited */ }
        }

        // ── FFmpeg Process ──────────────────────────────────────────
        private string _lastStderrLine = string.Empty;

        private async Task<int> RunFFmpegAsync(string arguments, CancellationToken ct)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            _currentProcess = process;

            process.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                _lastStderrLine = e.Data;
                OnRawOutput?.Invoke(e.Data);

                // Try to parse progress
                var progress = ParseProgress(e.Data);
                if (progress != null)
                    OnProgress?.Invoke(progress);
            };

            process.Start();
            process.BeginErrorReadLine();

            // Wait with cancellation support
            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Kill();
                throw;
            }

            _currentProcess = null;
            return process.ExitCode;
        }

        // ── Progress Parsing ────────────────────────────────────────
        // FFmpeg stderr format:
        // frame=  123 fps= 30 q=28.0 size=   1024kB time=00:00:05.12 bitrate=1638.4kbits/s speed=1.1x

        private static readonly Regex FrameRegex = new(@"frame=\s*(\d+)", RegexOptions.Compiled);
        private static readonly Regex FpsRegex = new(@"fps=\s*([\d.]+)", RegexOptions.Compiled);
        private static readonly Regex BitrateRegex = new(@"bitrate=\s*([\d.]+\w+/s)", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new(@"time=\s*([\d:.]+)", RegexOptions.Compiled);
        private static readonly Regex SpeedRegex = new(@"speed=\s*([\d.]+)x", RegexOptions.Compiled);
        private static readonly Regex SizeRegex = new(@"size=\s*(\d+\w+)", RegexOptions.Compiled);

        private static FFmpegProgress? ParseProgress(string line)
        {
            var timeMatch = TimeRegex.Match(line);
            if (!timeMatch.Success) return null; // Not a progress line

            var progress = new FFmpegProgress();

            if (TimeSpan.TryParse(timeMatch.Groups[1].Value, out var time))
                progress.Time = time;

            var frameMatch = FrameRegex.Match(line);
            if (frameMatch.Success && int.TryParse(frameMatch.Groups[1].Value, out int frame))
                progress.Frame = frame;

            var fpsMatch = FpsRegex.Match(line);
            if (fpsMatch.Success && double.TryParse(fpsMatch.Groups[1].Value, out double fps))
                progress.Fps = fps;

            var bitrateMatch = BitrateRegex.Match(line);
            if (bitrateMatch.Success)
                progress.Bitrate = bitrateMatch.Groups[1].Value;

            var speedMatch = SpeedRegex.Match(line);
            if (speedMatch.Success && double.TryParse(speedMatch.Groups[1].Value,  out double speed))
                progress.Speed = speed;

            var sizeMatch = SizeRegex.Match(line);
            if (sizeMatch.Success)
                progress.Size = sizeMatch.Groups[1].Value;

            return progress;
        }
    }

    /// <summary>
    /// Parsed progress data from a single FFmpeg stderr line.
    /// </summary>
    public class FFmpegProgress
    {
        public int Frame { get; set; }
        public double Fps { get; set; }
        public string Bitrate { get; set; } = "0kbits/s";
        public TimeSpan Time { get; set; }
        public double Speed { get; set; }
        public string Size { get; set; } = "0kB";
    }
}
