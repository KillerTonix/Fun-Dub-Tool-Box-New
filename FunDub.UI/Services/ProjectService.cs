using FunDub.UI.Models;
using FunDub.UI.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Saves and loads Fun Dub project files (.fundub).
    /// A project captures all materials, logo settings, and quality settings.
    /// </summary>
    public class ProjectService
    {
        /// <summary>
        /// Serializable project data.
        /// </summary>
        public class ProjectData
        {
            public string? IntroPath { get; set; }
            public string? VideoPath { get; set; }
            public string? LogoPath { get; set; }
            public string? SubtitlesPath { get; set; }
            public string? AudioPath { get; set; }
            public string? OutroPath { get; set; }

            // Logo
            public double LogoRelativeX { get; set; }
            public double LogoRelativeY { get; set; }
            public double LogoScale { get; set; } = 1.0;
            public double LogoOpacity { get; set; } = 1.0;
            public int LogoPositionIndex { get; set; } = 1; // TopRight default

            // Quality
            public string QualityPreset { get; set; } = "Source";
            public string? CustomResolution { get; set; }
            public string? CustomFps { get; set; }
            public string? CustomBitrate { get; set; }

            // Output
            public string? OutputFileName { get; set; }
        }

        /// <summary>
        /// Opens a Save dialog and writes the project to a .fundub file.
        /// </summary>
        public static bool SaveProject(ProjectData data)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Fun Dub Project (*.fundub)|*.fundub",
                Title = "Save Project",
                DefaultExt = ".fundub"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json);
                    return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Opens a file dialog and loads a .fundub project file.
        /// Returns null if cancelled or failed.
        /// </summary>
        public static ProjectData? LoadProject()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Fun Dub Project (*.fundub)|*.fundub",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    return JsonSerializer.Deserialize<ProjectData>(json);
                }
                catch { }
            }

            return null;
        }
    }
}
