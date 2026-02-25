using FunDub.UI.Managers;
using FunDub.UI.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace FunDub.UI.Managers
{
    public static class QueueManager
    {
        public static ObservableCollection<QueueItem> Items { get; } = new();

        private static readonly PipelineBuilder _builder = new();

        /// <summary>
        /// Adds a ProcessingJob to the queue, auto-generating its pipeline steps.
        /// </summary>
        public static void AddJob(ProcessingJob job)
        {
            var steps = _builder.Build(job);

            Items.Add(new QueueItem
            {
                FileName = Path.GetFileName(job.OutputPath),
                Job = job,
                Steps = steps,
                OutputPath = job.OutputPath,
                Status = "Pending",
                Progress = 0
            });
        }

        /// <summary>
        /// Legacy overload — wraps raw args in a single-step pipeline.
        /// Kept for backward compatibility during migration.
        /// </summary>
        public static void AddJob(string inputPath, string outputPath, string args)
        {
            Items.Add(new QueueItem
            {
                FileName = Path.GetFileName(inputPath),
                OutputPath = outputPath,
                FFmpegArguments = args,
                Steps =
                [
                    new PipelineStep
                    {
                        Label = "Processing",
                        Arguments = args,
                        OutputPath = outputPath,
                        IsTempFile = false
                    }
                ],
                Status = "Pending",
                Progress = 0
            });
        }

        public static void Clear()
        {
            Items.Clear();
        }

        public static void Remove(QueueItem item)
        {
            Items.Remove(item);
        }
    }
}
