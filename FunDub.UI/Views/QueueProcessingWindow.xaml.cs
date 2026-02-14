using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace FunDub.UI.Views
{
    /// <summary>
    /// Interaction logic for QueueProcessingWindow.xaml
    /// </summary>
    public partial class QueueProcessingWindow : Window
    {
        private DispatcherTimer _timer;
        private DateTime _startTime;

        public QueueProcessingWindow()
        {
            InitializeComponent();
        }

        private void StartProcessingButton_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void ParseFFmpegOutput(string data)
        {
            // Simple parsing logic using Regex or Split
            if (data.Contains("frame="))
            {
                Application.Current.Dispatcher.Invoke(() => {
                    // Update your StatBlocks
                    FPSLabel.Value = ExtractValue(data, "fps=", " ");
                    CurrentFrameLabel.Value = ExtractValue(data, "frame=", " ");
                    BitrateLabel.Value = ExtractValue(data, "bitrate=", " ");
                });
            }
        }

        private string ExtractValue(string source, string key, string endChar)
        {
            int start = source.IndexOf(key) + key.Length;
            int end = source.IndexOf(endChar, start);
            return end > 0 ? source.Substring(start, end - start).Trim() : "0";
        }

        private void StartTimer()
        {
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => {
                var elapsed = DateTime.Now - _startTime;
                ElapsedTimeLabel.Text = elapsed.ToString(@"hh\:mm\:ss");

                // Basic Remaining Time calculation (Logic based on percentage)
                if (OverallProgressBar.Value > 0)
                {
                    var totalEstimated = TimeSpan.FromTicks((long)(elapsed.Ticks / (OverallProgressBar.Value / 100)));
                    RemainingTimeLabel.Text = (totalEstimated - elapsed).ToString(@"hh\:mm\:ss");
                }
            };
            _timer.Start();
        }


        // Inside ProcessingQueueWindow.xaml.cs
        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            // FFmpeg output example: frame=  123 fps= 30 q=28.0 size=   1024kB time=00:00:05.12 bitrate=1638.4kbits/s speed=1.1x
            var frameMatch = Regex.Match(e.Data, @"frame=\s*(\d+)");
            var fpsMatch = Regex.Match(e.Data, @"fps=\s*(\d+)");
            var bitrateMatch = Regex.Match(e.Data, @"bitrate=\s*([\d\.]+\w+/s)");

            Dispatcher.Invoke(() =>
            {
                if (frameMatch.Success) CurrentFrameLabel.Value = frameMatch.Groups[1].Value;
                if (fpsMatch.Success) FPSLabel.Value = fpsMatch.Groups[1].Value;
                if (bitrateMatch.Success) BitrateLabel.Value = bitrateMatch.Groups[1].Value;

                // Update the ProgressBar based on the time processed vs total duration
                // OverallProgressBar.Value = ... 
            });
        }
    }
}
