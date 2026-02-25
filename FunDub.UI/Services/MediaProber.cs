using System.Diagnostics;
using System.Globalization;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Uses ffprobe to extract media metadata (duration, resolution, etc.)
    /// needed by the pipeline for progress calculation.
    /// </summary>
    public static class MediaProber
    {
        /// <summary>
        /// Gets the duration of a media file using ffprobe.
        /// Returns TimeSpan.Zero if probing fails.
        /// </summary>
        public static async Task<TimeSpan> GetDurationAsync(string filePath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
            catch
            {
                // ffprobe not found or file inaccessible — fail gracefully
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Gets the total estimated duration for a ProcessingJob
        /// (sum of all segments that will appear in the final output).
        /// </summary>
        public static async Task<TimeSpan> GetTotalDurationAsync(Models.ProcessingJob job)
        {
            var durations = new List<Task<TimeSpan>>();

            if (job.HasIntro) durations.Add(GetDurationAsync(job.IntroPath!));
            durations.Add(GetDurationAsync(job.VideoPath));
            if (job.HasOutro) durations.Add(GetDurationAsync(job.OutroPath!));

            var results = await Task.WhenAll(durations);

            var total = TimeSpan.Zero;
            foreach (var d in results)
                total += d;

            return total;
        }
    }
}
