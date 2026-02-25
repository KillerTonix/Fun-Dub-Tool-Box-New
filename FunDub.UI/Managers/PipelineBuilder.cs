using FunDub.UI.Models;
using FunDub.UI.Services;
using System.Globalization;
using System.IO;
using System.Text;

namespace FunDub.UI.Managers
{
    /// <summary>
    /// Generates a list of FFmpeg pipeline steps from a <see cref="ProcessingJob"/>.
    /// 
    /// Pipeline strategy:
    ///   Step 1 — Process the main video (logo overlay + audio replace + subtitle burn + encode).
    ///   Step 2 — If Intro/Outro exist, concat them around the processed video.
    ///            Logo appears only on the main video segment.
    /// </summary>
    public class PipelineBuilder
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public List<PipelineStep> Build(ProcessingJob job)
        {
            if (string.IsNullOrEmpty(job.VideoPath))
                throw new ArgumentException("VideoPath is required.");
            if (string.IsNullOrEmpty(job.OutputPath))
                throw new ArgumentException("OutputPath is required.");

            var steps = new List<PipelineStep>();
            string dir = Path.GetDirectoryName(job.OutputPath)!;
            string processedVideoPath;

            // ── Step 1: Process the main video ──────────────────────
            if (job.NeedsConcat)
            {
                string tempName = $"_fundub_processed_{Guid.NewGuid():N}.mp4";
                processedVideoPath = Path.Combine(dir, tempName);

                steps.Add(new PipelineStep
                {
                    Label = "Processing main video",
                    Arguments = BuildProcessingArgs(job, processedVideoPath),
                    OutputPath = processedVideoPath,
                    IsTempFile = true
                });
            }
            else
            {
                processedVideoPath = job.OutputPath;

                steps.Add(new PipelineStep
                {
                    Label = "Processing video",
                    Arguments = BuildProcessingArgs(job, processedVideoPath),
                    OutputPath = processedVideoPath,
                    IsTempFile = false
                });
            }

            // ── Steps 2+: Normalize intro/outro then concat ─────────
            if (job.NeedsConcat)
            {
                // Segments that will be concatenated (in order)
                var concatSegments = new List<string>();

                // Normalize intro to match processed video's format
                if (job.HasIntro)
                {
                    string normIntro = Path.Combine(dir, $"_fundub_intro_{Guid.NewGuid():N}.ts");
                    steps.Add(new PipelineStep
                    {
                        Label = "Preparing intro",
                        Arguments = BuildNormalizeArgs(job.IntroPath!, normIntro),
                        OutputPath = normIntro,
                        IsTempFile = true
                    });
                    concatSegments.Add(normIntro);
                }

                // Normalize the processed video to MPEG-TS
                string normVideo = Path.Combine(dir, $"_fundub_video_{Guid.NewGuid():N}.ts");
                steps.Add(new PipelineStep
                {
                    Label = "Preparing main video for concat",
                    Arguments = BuildNormalizeArgs(processedVideoPath, normVideo),
                    OutputPath = normVideo,
                    IsTempFile = true
                });
                concatSegments.Add(normVideo);

                // Normalize outro
                if (job.HasOutro)
                {
                    string normOutro = Path.Combine(dir, $"_fundub_outro_{Guid.NewGuid():N}.ts");
                    steps.Add(new PipelineStep
                    {
                        Label = "Preparing outro",
                        Arguments = BuildNormalizeArgs(job.OutroPath!, normOutro),
                        OutputPath = normOutro,
                        IsTempFile = true
                    });
                    concatSegments.Add(normOutro);
                }

                // Final concat using the concat protocol (works with MPEG-TS)
                string concatInput = string.Join("|", concatSegments);
                steps.Add(new PipelineStep
                {
                    Label = "Joining segments",
                    Arguments = $"-i \"concat:{concatInput}\" -c copy -movflags +faststart -y \"{job.OutputPath}\"",
                    OutputPath = job.OutputPath,
                    IsTempFile = false
                });
            }

            return steps;
        }

        // ────────────────────────────────────────────────────────────
        //  Step 1: Process main video (logo + audio + subs + encode)
        // ────────────────────────────────────────────────────────────
        private string BuildProcessingArgs(ProcessingJob job, string outputPath)
        {
            var args = new StringBuilder();
            var filters = new List<string>();
            int inputIndex = 0;

            // ── Inputs ──────────────────────────────────────────────
            int videoInput = inputIndex++;
            args.Append($"-i \"{job.VideoPath}\" ");

            int logoInput = -1;
            if (job.HasLogo)
            {
                logoInput = inputIndex++;
                args.Append($"-i \"{job.LogoPath}\" ");
            }

            int audioInput = -1;
            if (job.HasAudio)
            {
                audioInput = inputIndex++;
                args.Append($"-i \"{job.AudioPath}\" ");
            }

            // ── Build filter_complex ────────────────────────────────
            // Track the current video stream label
            string currentVideo = $"[{videoInput}:v]";
            bool hasFilterChain = false;

            // --- Subtitle burn ---
            if (job.HasSubtitles)
            {
                // subtitles filter needs escaped path (backslash colons on Windows)
                string escapedSubPath = job.SubtitlesPath!
                    .Replace("\\", "/")
                    .Replace(":", "\\:");

                filters.Add($"{currentVideo}subtitles='{escapedSubPath}'[subbed]");
                currentVideo = "[subbed]";
                hasFilterChain = true;
            }

            // --- Logo overlay ---
            if (job.HasLogo)
            {
                string scaleW = $"iw*{job.LogoScale.ToString("0.00", Inv)}";
                string logoLabel = $"[{logoInput}:v]";

                // Scale & set opacity for logo
                filters.Add(
                    $"{logoLabel}scale={scaleW}:-1," +
                    $"format=rgba," +
                    $"colorchannelmixer=aa={job.LogoOpacity.ToString("0.00", Inv)}[logo]"
                );

                // Calculate overlay position
                string posX, posY;
                if (job.LogoPosition == LogoPresetPosition.Manual)
                {
                    posX = $"main_w*{job.LogoRelativeX.ToString("0.00", Inv)}";
                    posY = $"main_h*{job.LogoRelativeY.ToString("0.00", Inv)}";
                }
                else
                {
                    (posX, posY) = job.LogoPosition switch
                    {
                        LogoPresetPosition.TopLeft     => ("10", "10"),
                        LogoPresetPosition.TopRight    => ("main_w-overlay_w-10", "10"),
                        LogoPresetPosition.BottomLeft  => ("10", "main_h-overlay_h-10"),
                        LogoPresetPosition.BottomRight => ("main_w-overlay_w-10", "main_h-overlay_h-10"),
                        _ => ("10", "10")
                    };
                }

                filters.Add($"{currentVideo}[logo]overlay={posX}:{posY}[overlaid]");
                currentVideo = "[overlaid]";
                hasFilterChain = true;
            }

            // --- Resolution scaling (Custom preset) ---
            if (job.QualityPreset == "Custom"
                && !string.IsNullOrEmpty(job.CustomResolution)
                && job.CustomResolution != "Original (Source)")
            {
                string targetRes = job.CustomResolution.Replace(" x ", ":");
                filters.Add($"{currentVideo}scale={targetRes}[scaled]");
                currentVideo = "[scaled]";
                hasFilterChain = true;
            }

            // ── Assemble -filter_complex ────────────────────────────
            if (hasFilterChain)
            {
                string filterStr = string.Join(";", filters);
                args.Append($"-filter_complex \"{filterStr}\" ");
                // Map the final labelled video stream
                args.Append($"-map \"{currentVideo}\" ");
            }
            else
            {
                // No filters — direct copy of video stream
                args.Append($"-map {videoInput}:v ");
            }

            // ── Audio mapping ───────────────────────────────────────
            if (job.HasAudio)
            {
                // Replace original audio with the provided audio track
                args.Append($"-map {audioInput}:a ");
                args.Append("-shortest ");  // Trim to shorter of video/audio
            }
            else
            {
                // Keep original audio
                args.Append($"-map {videoInput}:a? ");
            }

            // ── Encoding settings ───────────────────────────────────
            args.Append(BuildEncodingArgs(job));

            // Audio encoding (always re-encode audio to ensure compatibility)
            args.Append("-c:a aac -b:a 192k ");

            // ── Output ──────────────────────────────────────────────
            args.Append($"-y \"{outputPath}\"");

            return args.ToString();
        }

        // ────────────────────────────────────────────────────────────
        //  Normalize: Re-encode a segment to MPEG-TS for safe concat
        //  MPEG-TS supports the concat protocol natively, unlike MP4.
        // ────────────────────────────────────────────────────────────
        private static string BuildNormalizeArgs(string inputPath, string outputPath)
        {
            // Re-encode to h264 + aac in MPEG-TS container
            // -bsf:v h264_mp4toannexb is required for h264 in MPEG-TS
            return $"-i \"{inputPath}\" " +
                   $"-c:v libx264 -crf 18 -preset fast " +
                   $"-c:a aac -b:a 192k " +
                   $"-bsf:v h264_mp4toannexb " +
                   $"-f mpegts " +
                   $"-y \"{outputPath}\"";
        }

        // ────────────────────────────────────────────────────────────
        //  Shared: Video encoding arguments
        // ────────────────────────────────────────────────────────────
        private string BuildEncodingArgs(ProcessingJob job)
        {
            // Resolve Auto to the best available encoder
            var encoder = job.Encoder == HardwareEncoder.Auto
                ? Services.GpuDetector.GetBestEncoder()
                : job.Encoder;

            string enc = Services.GpuDetector.GetEncoderName(encoder);
            var sb = new StringBuilder();

            switch (job.QualityPreset)
            {
                case "Custom":
                    sb.Append($"-c:v {enc} ");
                    sb.Append(GetPresetArg(encoder, "medium"));
                    if (!string.IsNullOrEmpty(job.CustomBitrate))
                        sb.Append($"-b:v {job.CustomBitrate} ");
                    if (!string.IsNullOrEmpty(job.CustomFps) && job.CustomFps != "Same as Source")
                        sb.Append($"-r {job.CustomFps} ");
                    break;

                case "High":
                    sb.Append($"-c:v {enc} ");
                    sb.Append(GetPresetArg(encoder, "high"));
                    sb.Append(GetQualityArg(encoder, 18));
                    break;

                case "Low":
                    sb.Append($"-c:v {enc} ");
                    sb.Append(GetPresetArg(encoder, "fast"));
                    sb.Append(GetQualityArg(encoder, 28));
                    sb.Append("-s 854x480 ");
                    break;

                case "Source":
                    sb.Append($"-c:v {enc} ");
                    sb.Append(GetPresetArg(encoder, "medium"));
                    sb.Append(GetQualityArg(encoder, 16));
                    break;

                default: // Medium
                    sb.Append($"-c:v {enc} ");
                    sb.Append(GetPresetArg(encoder, "medium"));
                    sb.Append(GetQualityArg(encoder, 23));
                    break;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the correct preset flag for each encoder family.
        /// </summary>
        private static string GetPresetArg(HardwareEncoder encoder, string quality)
        {
            return encoder switch
            {
                HardwareEncoder.Nvenc => quality switch
                {
                    "high" => "-preset p7 ",
                    "fast" => "-preset p1 ",
                    _ => "-preset p4 "
                },
                HardwareEncoder.Amf => quality switch
                {
                    "high" => "-quality quality ",
                    "fast" => "-quality speed ",
                    _ => "-quality balanced "
                },
                HardwareEncoder.Qsv => quality switch
                {
                    "high" => "-preset veryslow ",
                    "fast" => "-preset veryfast ",
                    _ => "-preset medium "
                },
                _ => quality switch // Software (libx264)
                {
                    "high" => "-preset slow ",
                    "fast" => "-preset fast ",
                    _ => "-preset medium "
                }
            };
        }

        /// <summary>
        /// Returns the correct quality/CRF flag for each encoder family.
        /// </summary>
        private static string GetQualityArg(HardwareEncoder encoder, int crf)
        {
            return encoder switch
            {
                HardwareEncoder.Nvenc => $"-cq {crf} ",
                HardwareEncoder.Amf => $"-qp_i {crf} -qp_p {crf} ",
                HardwareEncoder.Qsv => $"-global_quality {crf} ",
                _ => $"-crf {crf} "
            };
        }
    }
}
