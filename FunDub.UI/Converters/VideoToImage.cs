using FFMpegCore;

namespace FunDub.UI.Converters
{
    class VideoToImage
    {
        public static async Task<string> LoadVideoPreviewAsync(string videoPath)
        {
            // Get video duration to pick a random frame
            var analysis = await FFProbe.AnalyseAsync(videoPath);
            double duration = analysis.Duration.TotalSeconds;

            Random rand = new();
            // Skip the first 2 seconds to avoid black intros
            double second = rand.Next(2, (int)duration);

            var outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".png");

            // Use Await to ensure the file is created before returning
            await FFMpeg.SnapshotAsync(videoPath, outputPath, captureTime: TimeSpan.FromSeconds(second));

            return outputPath;
        }
    }
}
