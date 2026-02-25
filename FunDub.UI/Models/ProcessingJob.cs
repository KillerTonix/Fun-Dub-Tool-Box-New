namespace FunDub.UI.Models
{
    /// <summary>
    /// Represents a complete rendering job with all materials and settings.
    /// </summary>
    public class ProcessingJob
    {
        // ── Material Paths ──────────────────────────────────────────
        public string? IntroPath { get; set; }
        public string VideoPath { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public string? SubtitlesPath { get; set; }
        public string? AudioPath { get; set; }
        public string? OutroPath { get; set; }
        public string OutputPath { get; set; } = string.Empty;

        // ── Logo Settings ───────────────────────────────────────────
        public double LogoRelativeX { get; set; }
        public double LogoRelativeY { get; set; }
        public double LogoScale { get; set; } = 1.0;
        public double LogoOpacity { get; set; } = 1.0;
        public LogoPresetPosition LogoPosition { get; set; } = LogoPresetPosition.Manual;

        // ── Quality / Encoding ──────────────────────────────────────
        public string QualityPreset { get; set; } = "Medium";
        public string? CustomResolution { get; set; }
        public string? CustomFps { get; set; }
        public string? CustomBitrate { get; set; }

        // ── Hardware Acceleration ───────────────────────────────────
        public HardwareEncoder Encoder { get; set; } = HardwareEncoder.Software;

        // ── Computed helpers ────────────────────────────────────────
        public bool HasIntro => !string.IsNullOrEmpty(IntroPath);
        public bool HasOutro => !string.IsNullOrEmpty(OutroPath);
        public bool HasLogo => !string.IsNullOrEmpty(LogoPath);
        public bool HasSubtitles => !string.IsNullOrEmpty(SubtitlesPath);
        public bool HasAudio => !string.IsNullOrEmpty(AudioPath);
        public bool NeedsConcat => HasIntro || HasOutro;
    }

    public enum HardwareEncoder
    {
        Software,       // libx264 / libx265
        Nvenc,          // NVIDIA — h264_nvenc
        Amf,            // AMD    — h264_amf
        Qsv,            // Intel  — h264_qsv
        Auto            // Detect at runtime
    }

    public enum LogoPresetPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Manual          // Uses LogoRelativeX/Y
    }
}
