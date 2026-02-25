using FunDub.UI.Managers;
using FunDub.UI.Models;
using FunDub.UI.Services;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FunDub.UI.Views
{
    public partial class QueueProcessingWindow : Window
    {
        private readonly PipelineRunner _runner = new();
        private readonly UserSettings _settings;
        private readonly SystemMonitor _sysMonitor = new(intervalMs: 1000);
        private readonly List<HardwareEncoder> _availableEncoders;
        private CancellationTokenSource? _cts;
        private DispatcherTimer? _timer;
        private DateTime _startTime;
        private TimeSpan _totalDuration;
        private bool _isProcessing;
        private int _completedCount;
        private int _errorCount;

        public QueueProcessingWindow()
        {
            InitializeComponent();
            QueueListView.ItemsSource = QueueManager.Items;

            // ── Populate GPU combobox dynamically ───────────────────
            _availableEncoders = GpuDetector.GetAvailableEncoders();
            // Always add Auto as first option
            RenderingEngineComboBox.Items.Add(new ComboBoxItem { Content = "Auto-detect (Best)" });
            foreach (var enc in _availableEncoders)
            {
                RenderingEngineComboBox.Items.Add(new ComboBoxItem
                {
                    Content = GpuDetector.GetDisplayName(enc)
                });
            }

            // Load saved settings
            _settings = UserSettings.Load();
            if (_settings.RenderingEngineIndex < RenderingEngineComboBox.Items.Count)
                RenderingEngineComboBox.SelectedIndex = _settings.RenderingEngineIndex;
            else
                RenderingEngineComboBox.SelectedIndex = 0;

            RenderingEngineComboBox.SelectionChanged += (_, _) =>
            {
                _settings.RenderingEngineIndex = RenderingEngineComboBox.SelectedIndex;
                _settings.Save();
            };

            // Wire up pipeline events
            _runner.OnStepStarted += Runner_OnStepStarted;
            _runner.OnProgress += Runner_OnProgress;
            _runner.OnCompleted += Runner_OnCompleted;
            _runner.OnError += Runner_OnError;

            // Wire up system monitor
            _sysMonitor.OnMetricsUpdated += metrics =>
            {
                Dispatcher.Invoke(() =>
                {
                    CPUUsageLabel.Value = $"{metrics.CpuPercent}%";
                    GPUUsageLabel.Value = $"{metrics.GpuPercent}%";
                    RAMUsageLabel.Value = $"{metrics.RamMb} MB";
                });
            };

            this.Closed += (_, _) => _sysMonitor.Dispose();
        }

        /// <summary>
        /// Returns the HardwareEncoder selected in the combobox.
        /// Index 0 = Auto, Index 1+ maps to _availableEncoders[i-1]
        /// </summary>
        private HardwareEncoder GetSelectedEncoder()
        {
            int idx = RenderingEngineComboBox.SelectedIndex;
            if (idx <= 0) return HardwareEncoder.Auto;
            if (idx - 1 < _availableEncoders.Count) return _availableEncoders[idx - 1];
            return HardwareEncoder.Software;
        }

        // ── Edit / Clear Buttons ────────────────────────────────────
        private void EditSelectedQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Cannot edit while processing.", "Busy",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = QueueListView.SelectedItem as QueueItem;
            if (selected == null)
            {
                MessageBox.Show("Select an item from the queue to edit.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selected.Status != "Pending")
            {
                MessageBox.Show("Only pending items can be edited.", "Cannot Edit",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            QueueManager.Remove(selected);
            MessageBox.Show(
                $"Removed \"{selected.FileName}\" from queue.\n\n" +
                "Adjust your settings in the main window and add it back.",
                "Item Removed for Editing", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Cannot clear while processing.", "Busy",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count = QueueManager.Items.Count;
            if (count == 0) return;

            var result = MessageBox.Show(
                $"Remove all {count} item(s) from the queue?",
                "Clear Queue", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                QueueManager.Clear();
        }

        // ── Start / Stop ────────────────────────────────────────────
        private async void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing)
            {
                _cts?.Cancel();
                return;
            }

            _isProcessing = true;
            _completedCount = 0;
            _errorCount = 0;
            StartTextBlock.Text = "Stop";
            _cts = new CancellationTokenSource();

            StartTimer();
            _sysMonitor.Start();

            // Get selected encoder from combobox
            var selectedEncoder = GetSelectedEncoder();

            int totalJobs = QueueManager.Items.Count(i => i.Status == "Pending");

            foreach (var item in QueueManager.Items)
            {
                if (item.Status != "Pending") continue;

                // Apply selected encoder to job and rebuild steps
                if (item.Job != null)
                {
                    item.Job.Encoder = selectedEncoder;
                    item.Steps = new PipelineBuilder().Build(item.Job);
                }

                item.Status = "Processing";
                item.Progress = 0;

                Dispatcher.Invoke(() =>
                    CurrentRenderingFileLabel.Text = item.FileName);

                _totalDuration = item.Job != null
                    ? await MediaProber.GetTotalDurationAsync(item.Job)
                    : TimeSpan.Zero;

                try
                {
                    await _runner.RunAsync(item.Steps, _cts.Token);
                    item.Status = "Done ✓";
                    item.Progress = 100;
                    _completedCount++;
                }
                catch (OperationCanceledException)
                {
                    item.Status = "Cancelled";
                    break;
                }
            }

            _sysMonitor.Stop();
            StopTimer();
            _isProcessing = false;
            StartTextBlock.Text = "Start";

            Dispatcher.Invoke(() =>
            {
                CPUUsageLabel.Value = "0%";
                GPUUsageLabel.Value = "0%";
                RAMUsageLabel.Value = "0 MB";
            });

            ShowFinishingSummary(totalJobs);
        }

        // ── Finishing Summary ───────────────────────────────────────
        private void ShowFinishingSummary(int totalJobs)
        {
            var elapsed = DateTime.Now - _startTime;
            string elapsedStr = elapsed.ToString(@"hh\:mm\:ss");

            Dispatcher.Invoke(() =>
            {
                CurrentRenderingFileLabel.Text = "All tasks finished!";
                SetProgress(100);
                RemainingTimeLabel.Text = "00:00:00";
            });

            string summary;
            MessageBoxImage icon;

            if (_errorCount == 0 && _completedCount > 0)
            {
                summary = $"All {_completedCount} job(s) completed successfully!\n\nTotal time: {elapsedStr}";
                icon = MessageBoxImage.Information;
                SystemSounds.Asterisk.Play();
            }
            else if (_completedCount > 0 && _errorCount > 0)
            {
                summary = $"Finished with errors.\n\n" +
                          $"  ✓  Completed: {_completedCount}\n" +
                          $"  ✗  Failed: {_errorCount}\n\nTotal time: {elapsedStr}";
                icon = MessageBoxImage.Warning;
                SystemSounds.Exclamation.Play();
            }
            else if (_completedCount == 0 && _errorCount > 0)
            {
                summary = $"All {_errorCount} job(s) failed.\n\nTotal time: {elapsedStr}";
                icon = MessageBoxImage.Error;
                SystemSounds.Hand.Play();
            }
            else
            {
                summary = $"Processing stopped.\n\n" +
                          $"  ✓  Completed: {_completedCount}\n" +
                          $"  —  Remaining: {totalJobs - _completedCount - _errorCount}\n\nElapsed: {elapsedStr}";
                icon = MessageBoxImage.Information;
            }

            if (AutomaticShutdownPC.IsChecked == true && _completedCount > 0)
            {
                var result = MessageBox.Show(summary + "\n\nShut down the PC now?",
                    "Processing Complete — Shutdown", MessageBoxButton.YesNo, icon);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("shutdown", "/s /t 30");
                    return;
                }
            }
            else
            {
                string? firstOutput = QueueManager.Items
                    .FirstOrDefault(i => i.Status == "Done ✓")?.OutputPath;

                if (!string.IsNullOrEmpty(firstOutput))
                {
                    string? dir = System.IO.Path.GetDirectoryName(firstOutput);
                    var result = MessageBox.Show(summary + "\n\nOpen output folder?",
                        "Processing Complete", MessageBoxButton.YesNo, icon);
                    if (result == MessageBoxResult.Yes && dir != null)
                        Process.Start("explorer.exe", dir);
                }
                else
                {
                    MessageBox.Show(summary, "Processing Complete", MessageBoxButton.OK, icon);
                }
            }
        }

        // ── Pipeline Events ─────────────────────────────────────────
        private void Runner_OnStepStarted(int stepIndex, int totalSteps, string label) =>
            Dispatcher.Invoke(() =>
            {
                var current = QueueManager.Items.FirstOrDefault(i => i.Status == "Processing");
                if (current != null)
                    current.CurrentStep = $"Step {stepIndex + 1}/{totalSteps}: {label}";
            });

        private void Runner_OnProgress(FFmpegProgress p) =>
            Dispatcher.Invoke(() =>
            {
                FPSLabel.Value = p.Fps.ToString("0");
                CurrentFrameLabel.Value = p.Frame.ToString();
                BitrateLabel.Value = p.Bitrate;

                if (_totalDuration.TotalSeconds > 0)
                {
                    double pct = Math.Clamp((p.Time.TotalSeconds / _totalDuration.TotalSeconds) * 100, 0, 100);
                    SetProgress(pct);
                    var current = QueueManager.Items.FirstOrDefault(i => i.Status == "Processing");
                    if (current != null) current.Progress = (int)pct;
                }

                if (p.Speed > 0 && _totalDuration.TotalSeconds > 0)
                {
                    double rem = (_totalDuration.TotalSeconds - p.Time.TotalSeconds) / p.Speed;
                    if (rem > 0) RemainingTimeLabel.Text = TimeSpan.FromSeconds(rem).ToString(@"hh\:mm\:ss");
                }
            });

        private void Runner_OnCompleted() { }

        private void Runner_OnError(int stepIndex, int exitCode, string lastOutput)
        {
            _errorCount++;
            Dispatcher.Invoke(() =>
            {
                var current = QueueManager.Items.FirstOrDefault(i => i.Status == "Processing");
                if (current != null) current.Status = $"Error (exit {exitCode})";
                MessageBox.Show($"FFmpeg failed at step {stepIndex + 1} with exit code {exitCode}.\n\n{lastOutput}",
                    "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        // ── Timer ───────────────────────────────────────────────────
        private void StartTimer()
        {
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => ElapsedTimeLabel.Text = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
            _timer.Start();
        }

        private void StopTimer() { _timer?.Stop(); _timer = null; }

        /// <summary>Sets progress on both the hidden ProgressBar and the visual gradient border.</summary>
        private void SetProgress(double pct)
        {
            pct = Math.Clamp(pct, 0, 100);
            OverallProgressBar.Value = pct;
            // Animate the visual border width (parent border is the container)
            if (OverallProgressBarBorder.Parent is Border container)
            {
                double maxWidth = container.ActualWidth > 0 ? container.ActualWidth : 400;
                OverallProgressBarBorder.Width = maxWidth * (pct / 100.0);
            }
        }
    }
}
