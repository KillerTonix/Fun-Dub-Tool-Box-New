using System.Text;

namespace FunDub.UI.Managers
{
    public class FFmpegCommandBuilder
    {
        public string Build(string videoPath, string logoPath, string outputPath,
                            double logoRelX, double logoRelY, double logoScale, double logoOpacity,
                            string qualityPreset, string customRes, string customFps, string customBitrate)
        {
            var args = new StringBuilder();

            // 1. Inputs
            args.Append($"-i \"{videoPath}\" ");
            if (!string.IsNullOrEmpty(logoPath))
            {
                args.Append($"-i \"{logoPath}\" ");
            }

            // 2. Filter Complex (The Magic)
            // We use a filter chain: [logo] -> Scale -> Opacity -> Overlay
            string filter = "";

            if (!string.IsNullOrEmpty(logoPath))
            {
                // Scale and Opacity for Logo
                // [1:v] refers to the 2nd input (the logo)
                filter += $"[1:v]scale=iw*{logoScale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}:-1,";
                filter += $"format=rgba,colorchannelmixer=aa={logoOpacity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}[logo];";

                // Overlay logic
                // main_w * relativeX calculates position regardless of resolution
                string posX = $"main_w*{logoRelX.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";
                string posY = $"main_h*{logoRelY.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";

                filter += $"[0:v][logo]overlay={posX}:{posY}";
            }
            else
            {
                // If no logo, just pass video through (or scaling filters if needed)
                filter += "[0:v]null";
            }

            // 3. Custom Resolution Scaling (if not "Source")
            if (qualityPreset == "Custom" && customRes != "Original (Source)")
            {
                // Add a scale filter to the end of the chain
                // Note: If we already have an overlay, we need to pipe that output to scale
                string targetRes = customRes.Replace(" x ", ":");
                filter += $",scale={targetRes}";
            }

            args.Append($"-filter_complex \"{filter}\" ");

            // 4. Encoding Settings
            if (qualityPreset == "Custom")
            {
                args.Append("-c:v libx264 -preset medium ");
                if (!string.IsNullOrEmpty(customBitrate)) args.Append($"-b:v {customBitrate} ");
                if (customFps != "Same as Source") args.Append($"-r {customFps} ");
            }
            else
            {
                // Presets
                args.Append(qualityPreset switch
                {
                    "High" => "-c:v libx264 -crf 18 -preset slow ",
                    "Medium" => "-c:v libx264 -crf 23 -preset medium ",
                    "Low" => "-c:v libx264 -crf 28 -s 854x480 ",
                    _ => "-c:v copy " // Source
                });
            }

            // 5. Output
            args.Append($"-y \"{outputPath}\"");

            return args.ToString();
        }
    }
}
