using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Periodically extracts a frame from the video currently being written
    /// by FFmpeg, for the preview panel in the processing window.
    /// 
    /// Strategy: Reads the in-progress output file at a given timestamp.
    /// Falls back to the source video if the output isn't readable yet.
    /// </summary>
    public class PreviewService : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private string _currentVideoPath = string.Empty;
        private string _sourceVideoPath = string.Empty;
        private TimeSpan _currentTime = TimeSpan.Zero;
        private bool _isBusy;
        private bool _disposed;
        private string? _lastFramePath;

        /// <summary>Fires with a BitmapImage of the extracted frame.</summary>
        public event Action<BitmapImage>? OnFrameReady;

        public PreviewService(int intervalMs = 3000)
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += async (_, _) => await ExtractFrameAsync();
        }

        /// <summary>
        /// Sets the video paths for frame extraction.
        /// </summary>
        /// <param name="outputPath">The file FFmpeg is currently writing to.</param>
        /// <param name="sourcePath">The original source video (fallback).</param>
        public void SetVideo(string outputPath, string sourcePath)
        {
            _currentVideoPath = outputPath;
            _sourceVideoPath = sourcePath;
        }

        /// <summary>
        /// Updates the current processing time so we extract a relevant frame.
        /// </summary>
        public void UpdateTime(TimeSpan time)
        {
            _currentTime = time;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private async Task ExtractFrameAsync()
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                // Clean up previous frame
                CleanupLastFrame();

                // Try the source video at the current timestamp
                string videoToProbe = !string.IsNullOrEmpty(_sourceVideoPath) && File.Exists(_sourceVideoPath)
                    ? _sourceVideoPath
                    : _currentVideoPath;

                if (string.IsNullOrEmpty(videoToProbe) || !File.Exists(videoToProbe))
                    return;

                string framePath = Path.Combine(Path.GetTempPath(), $"_fundub_preview_{Guid.NewGuid():N}.jpg");

                // Use ffmpeg to extract a single frame
                // Seek to slightly before current time for speed (input seeking)
                double seekSec = Math.Max(0, _currentTime.TotalSeconds - 1);
                string args = $"-ss {seekSec:F2} -i \"{videoToProbe}\" -frames:v 1 -q:v 5 -y \"{framePath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return;

                await proc.WaitForExitAsync();

                if (proc.ExitCode == 0 && File.Exists(framePath))
                {
                    _lastFramePath = framePath;

                    // Load as BitmapImage (must be done with stream to avoid file locks)
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(framePath);
                    bitmap.EndInit();
                    bitmap.Freeze(); // Allow cross-thread access

                    OnFrameReady?.Invoke(bitmap);
                }
            }
            catch
            {
                // Non-critical — skip this frame
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void CleanupLastFrame()
        {
            if (_lastFramePath != null)
            {
                try { if (File.Exists(_lastFramePath)) File.Delete(_lastFramePath); }
                catch { /* file may be in use */ }
                _lastFramePath = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
            CleanupLastFrame();
        }
    }
}
