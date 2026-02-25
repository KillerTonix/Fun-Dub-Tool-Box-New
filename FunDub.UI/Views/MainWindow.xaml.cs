using FunDub.UI.Converters;
using FunDub.UI.Managers;
using FunDub.UI.Models;
using FunDub.UI.Services;
using FunDub.UI.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FunDub.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainService _mainService = new();
        private readonly UserSettings _settings;
        public ObservableCollection<MaterialItem> _materials = MainViewModel.Materials;

        private double _logoRelativeX = 0;
        private double _logoRelativeY = 0;
        private double _logoScale = 1.0;
        private double _logoOpacity = 1.0;
        private LogoPresetPosition _logoPosition = LogoPresetPosition.TopRight;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();

            // Load saved settings and restore saved materials
            _settings = UserSettings.Load();
            RestoreSavedMaterials();
        }

        // ── Restore saved intro/logo/outro on startup ───────────────
        private void RestoreSavedMaterials()
        {
            if (!string.IsNullOrEmpty(_settings.SavedIntroPath) && File.Exists(_settings.SavedIntroPath))
            {
                _materials.Add(new MaterialItem { Type = "Intro", Path = _settings.SavedIntroPath });
                IntroTB.IsChecked = true;
                IntroSaveTB.IsChecked = true;
            }

            if (!string.IsNullOrEmpty(_settings.SavedLogoPath) && File.Exists(_settings.SavedLogoPath))
            {
                _materials.Add(new MaterialItem { Type = "Logo", Path = _settings.SavedLogoPath });
                LogoTB.IsChecked = true;
                LogoSaveTB.IsChecked = true;
                LogoSettingsGB.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(_settings.SavedOutroPath) && File.Exists(_settings.SavedOutroPath))
            {
                _materials.Add(new MaterialItem { Type = "Outro", Path = _settings.SavedOutroPath });
                OutroTB.IsChecked = true;
                OutroSaveTB.IsChecked = true;
            }

            // Restore logo settings
            _logoOpacity = _settings.SavedLogoOpacity;
            LogoTransparencyBox.Text = $"{(int)(_logoOpacity * 100)}%";
        }

        // ═══════════════════════════════════════════════════════════
        //  Material Selection
        // ═══════════════════════════════════════════════════════════

        private void IntroTB_Click(object sender, RoutedEventArgs e)
        {
            if (IntroTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Intro"))
                    IntroTB.IsChecked = false;
            }
            else _mainService.RemoveFileSelector("Intro");
        }

        private void VideoTB_Click(object sender, RoutedEventArgs e)
        {
            if (VideoTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Video"))
                    VideoTB.IsChecked = false;
                var videoItem = _materials.FirstOrDefault(m => m.Type == "Video");
                if (videoItem != null && !string.IsNullOrEmpty(videoItem.Path))
                    OutputFileNameBox.Text = Path.GetFileNameWithoutExtension(videoItem.Path) + "_dubbed";
            }
            else _mainService.RemoveFileSelector("Video");
        }

        private void LogoTB_Click(object sender, RoutedEventArgs e)
        {
            if (LogoTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Logo"))
                    LogoTB.IsChecked = false;
                LogoSettingsGB.Visibility = Visibility.Visible;
            }
            else
            {
                _mainService.RemoveFileSelector("Logo");
                LogoSettingsGB.Visibility = Visibility.Hidden;
            }
        }

        private void SubtitlesTB_Click(object sender, RoutedEventArgs e)
        {
            if (SubtitlesTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Subtitles"))
                    SubtitlesTB.IsChecked = false;
            }
            else _mainService.RemoveFileSelector("Subtitles");
        }

        private void AudioTB_Click(object sender, RoutedEventArgs e)
        {
            if (AudioTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Audio"))
                    AudioTB.IsChecked = false;
            }
            else _mainService.RemoveFileSelector("Audio");
        }

        private void OutroTB_Click(object sender, RoutedEventArgs e)
        {
            if (OutroTB.IsChecked == true)
            {
                if (!_mainService.OpenFileSelector("Outro"))
                    OutroTB.IsChecked = false;
            }
            else _mainService.RemoveFileSelector("Outro");
        }

        // ═══════════════════════════════════════════════════════════
        //  Save Buttons (💾) — persist material paths across sessions
        // ═══════════════════════════════════════════════════════════

        private void IntroSaveTB_Click(object sender, RoutedEventArgs e)
        {
            if (IntroSaveTB.IsChecked == true)
            {
                string? path = _materials.FirstOrDefault(m => m.Type == "Intro")?.Path;
                _settings.SavedIntroPath = path;
            }
            else
            {
                _settings.SavedIntroPath = null;
            }
            _settings.Save();
        }

        private void LogoSaveTB_Click(object sender, RoutedEventArgs e)
        {
            if (LogoSaveTB.IsChecked == true)
            {
                string? path = _materials.FirstOrDefault(m => m.Type == "Logo")?.Path;
                _settings.SavedLogoPath = path;
                _settings.SavedLogoOpacity = _logoOpacity;
            }
            else
            {
                _settings.SavedLogoPath = null;
            }
            _settings.Save();
        }

        private void OutroSaveTB_Click(object sender, RoutedEventArgs e)
        {
            if (OutroSaveTB.IsChecked == true)
            {
                string? path = _materials.FirstOrDefault(m => m.Type == "Outro")?.Path;
                _settings.SavedOutroPath = path;
            }
            else
            {
                _settings.SavedOutroPath = null;
            }
            _settings.Save();
        }

        // ═══════════════════════════════════════════════════════════
        //  Logo Settings
        // ═══════════════════════════════════════════════════════════

        private void LogoPositionRB_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                if (rb == LogoPosTopLeft) _logoPosition = LogoPresetPosition.TopLeft;
                else if (rb == LogoPosTopRight) _logoPosition = LogoPresetPosition.TopRight;
                else if (rb == LogoPosBotLeft) _logoPosition = LogoPresetPosition.BottomLeft;
                else if (rb == LogoPosBotRight) _logoPosition = LogoPresetPosition.BottomRight;
            }
        }

        private void ManualRB_Checked(object sender, RoutedEventArgs e)
        {
            if (ManualRB.IsChecked == true)
            {
                _logoPosition = LogoPresetPosition.Manual;

                LogoPositioningWindow logoPositioningWindow = new()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                if (logoPositioningWindow.ShowDialog() == true)
                {
                    _logoRelativeX = logoPositioningWindow.RelativeX;
                    _logoRelativeY = logoPositioningWindow.RelativeY;
                    _logoScale = logoPositioningWindow.LogoScale;
                    _logoOpacity = logoPositioningWindow.LogoOpacity;
                    LogoTransparencyBox.Text = $"{(int)(_logoOpacity * 100)}%";
                }
            }
        }

        private void LogoTransparencyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string text = LogoTransparencyBox.Text.Replace("%", "").Trim();
            if (int.TryParse(text, out int pct))
            {
                pct = Math.Clamp(pct, 0, 100);
                _logoOpacity = pct / 100.0;
                LogoTransparencyBox.Text = $"{pct}%";
            }
            else
            {
                LogoTransparencyBox.Text = $"{(int)(_logoOpacity * 100)}%";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Queue & Processing
        // ═══════════════════════════════════════════════════════════

        private void SeeQueueListButton_Click(object sender, RoutedEventArgs e)
        {
            var queueWindow = new QueueProcessingWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            queueWindow.Show();
        }

        private void FirstStratButton_Click(object sender, RoutedEventArgs e)
        {
            // Add current project to queue, then open processing window
            AddToQueueButton_Click(sender, e);

            var renderingWindow = new QueueProcessingWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            renderingWindow.Show();
        }

        private void QualityCustom_Checked(object sender, RoutedEventArgs e)
        {
            // Just toggle the custom settings panel — don't auto-start
        }

        private string GetSelectedQuality()
        {
            if (QualityCustom.IsChecked == true) return "Custom";
            if (QualityHigh.IsChecked == true) return "High";
            if (QualityMed.IsChecked == true) return "Medium";
            if (QualityLow.IsChecked == true) return "Low";
            return "Source";
        }

        private void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            string? videoPath = _materials.FirstOrDefault(m => m.Type == "Video")?.Path;
            if (string.IsNullOrEmpty(videoPath))
            {
                MessageBox.Show("No video selected!", "Missing Material", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dir = Path.GetDirectoryName(videoPath)!;
            string outName;
            if (!string.IsNullOrWhiteSpace(OutputFileNameBox.Text))
            {
                outName = OutputFileNameBox.Text.Trim();
                if (!outName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    outName += ".mp4";
            }
            else
            {
                outName = Path.GetFileNameWithoutExtension(videoPath) + "_dubbed.mp4";
            }
            string outPath = Path.Combine(dir, outName);

            var job = new ProcessingJob
            {
                VideoPath = videoPath,
                IntroPath = _materials.FirstOrDefault(m => m.Type == "Intro")?.Path,
                OutroPath = _materials.FirstOrDefault(m => m.Type == "Outro")?.Path,
                LogoPath = _materials.FirstOrDefault(m => m.Type == "Logo")?.Path,
                SubtitlesPath = _materials.FirstOrDefault(m => m.Type == "Subtitles")?.Path,
                AudioPath = _materials.FirstOrDefault(m => m.Type == "Audio")?.Path,
                OutputPath = outPath,

                LogoPosition = _logoPosition,
                LogoRelativeX = _logoRelativeX,
                LogoRelativeY = _logoRelativeY,
                LogoScale = _logoScale,
                LogoOpacity = _logoOpacity,

                QualityPreset = GetSelectedQuality(),
                CustomResolution = CustomResolutionCombo.Text,
                CustomFps = CustomFpsCombo.Text,
                CustomBitrate = CustomBitrateBox.Text,

                Encoder = HardwareEncoder.Auto
            };

            QueueManager.AddJob(job);

            OutputFileNameBox.Text = "";
            ItemsInQueueLabel.Content = $"Items in queue: {QueueManager.Items.Count}";
            MessageBox.Show($"Added to Queue!\n{(job.NeedsConcat ? "Will concatenate Intro/Outro segments." : "Single video processing.")}", "Queued");
        }

        // ═══════════════════════════════════════════════════════════
        //  Project Save / Load
        // ═══════════════════════════════════════════════════════════

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var data = new ProjectService.ProjectData
            {
                IntroPath = _materials.FirstOrDefault(m => m.Type == "Intro")?.Path,
                VideoPath = _materials.FirstOrDefault(m => m.Type == "Video")?.Path,
                LogoPath = _materials.FirstOrDefault(m => m.Type == "Logo")?.Path,
                SubtitlesPath = _materials.FirstOrDefault(m => m.Type == "Subtitles")?.Path,
                AudioPath = _materials.FirstOrDefault(m => m.Type == "Audio")?.Path,
                OutroPath = _materials.FirstOrDefault(m => m.Type == "Outro")?.Path,

                LogoRelativeX = _logoRelativeX,
                LogoRelativeY = _logoRelativeY,
                LogoScale = _logoScale,
                LogoOpacity = _logoOpacity,
                LogoPositionIndex = (int)_logoPosition,

                QualityPreset = GetSelectedQuality(),
                CustomResolution = CustomResolutionCombo.Text,
                CustomFps = CustomFpsCombo.Text,
                CustomBitrate = CustomBitrateBox.Text,

                OutputFileName = OutputFileNameBox.Text
            };

            if (ProjectService.SaveProject(data))
                MessageBox.Show("Project saved!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var data = ProjectService.LoadProject();
            if (data == null) return;

            // Clear current materials
            _materials.Clear();
            IntroTB.IsChecked = false;
            VideoTB.IsChecked = false;
            LogoTB.IsChecked = false;
            SubtitlesTB.IsChecked = false;
            AudioTB.IsChecked = false;
            OutroTB.IsChecked = false;
            LogoSettingsGB.Visibility = Visibility.Hidden;

            // Restore materials
            void AddMat(string type, string? path, ToggleButton tb)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _materials.Add(new MaterialItem { Type = type, Path = path });
                    tb.IsChecked = true;
                }
            }

            AddMat("Intro", data.IntroPath, IntroTB);
            AddMat("Video", data.VideoPath, VideoTB);
            AddMat("Logo", data.LogoPath, LogoTB);
            AddMat("Subtitles", data.SubtitlesPath, SubtitlesTB);
            AddMat("Audio", data.AudioPath, AudioTB);
            AddMat("Outro", data.OutroPath, OutroTB);

            if (LogoTB.IsChecked == true)
                LogoSettingsGB.Visibility = Visibility.Visible;

            // Restore logo settings
            _logoRelativeX = data.LogoRelativeX;
            _logoRelativeY = data.LogoRelativeY;
            _logoScale = data.LogoScale;
            _logoOpacity = data.LogoOpacity;
            _logoPosition = (LogoPresetPosition)data.LogoPositionIndex;
            LogoTransparencyBox.Text = $"{(int)(_logoOpacity * 100)}%";

            // Set logo position radio button
            switch (_logoPosition)
            {
                case LogoPresetPosition.TopLeft: LogoPosTopLeft.IsChecked = true; break;
                case LogoPresetPosition.TopRight: LogoPosTopRight.IsChecked = true; break;
                case LogoPresetPosition.BottomLeft: LogoPosBotLeft.IsChecked = true; break;
                case LogoPresetPosition.BottomRight: LogoPosBotRight.IsChecked = true; break;
                case LogoPresetPosition.Manual: ManualRB.IsChecked = true; break;
            }

            // Restore quality
            switch (data.QualityPreset)
            {
                case "Custom": QualityCustom.IsChecked = true; break;
                case "Low": QualityLow.IsChecked = true; break;
                case "Medium": QualityMed.IsChecked = true; break;
                case "High": QualityHigh.IsChecked = true; break;
                default: QualitySource.IsChecked = true; break;
            }

            CustomBitrateBox.Text = data.CustomBitrate ?? "5000";
            OutputFileNameBox.Text = data.OutputFileName ?? "";

            MessageBox.Show("Project loaded!", "Load", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═══════════════════════════════════════════════════════════
        //  Drag & Drop (stubs)
        // ═══════════════════════════════════════════════════════════
        private void MaterialsLB_DragEnter(object sender, DragEventArgs e) { }
        private void MaterialsLB_DragLeave(object sender, DragEventArgs e) { }
        private void MaterialsLB_Drop(object sender, DragEventArgs e) { }
    }
}
