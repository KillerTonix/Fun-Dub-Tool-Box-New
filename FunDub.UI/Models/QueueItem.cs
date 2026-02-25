using CommunityToolkit.Mvvm.ComponentModel;

namespace FunDub.UI.Models
{
    public partial class QueueItem : ObservableObject
    {
        [ObservableProperty] private string _fileName = string.Empty;
        [ObservableProperty] private string _status = "Pending";     // Pending → Processing → Done / Error
        [ObservableProperty] private int _progress;
        [ObservableProperty] private string _currentStep = string.Empty;

        /// <summary>The full job definition (materials + settings).</summary>
        public ProcessingJob Job { get; set; } = new();

        /// <summary>Pre-built pipeline steps generated from the Job.</summary>
        public List<PipelineStep> Steps { get; set; } = [];

        // ── Legacy compat (keep for now) ────────────────────────────
        public string FFmpegArguments { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
    }
}
