namespace FunDub.UI.Models
{
    /// <summary>
    /// A single FFmpeg command in a multi-step pipeline.
    /// The pipeline runner executes these in order.
    /// </summary>
    public class PipelineStep
    {
        public string Label { get; set; } = string.Empty;       // e.g. "Processing video", "Concatenating"
        public string Arguments { get; set; } = string.Empty;   // Full FFmpeg argument string
        public string OutputPath { get; set; } = string.Empty;  // Output of this step
        public bool IsTempFile { get; set; }                    // If true, delete after pipeline completes
    }
}
