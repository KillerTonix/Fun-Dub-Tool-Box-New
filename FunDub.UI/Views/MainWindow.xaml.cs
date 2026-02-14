using FunDub.UI.Converters;
using FunDub.UI.Services;
using FunDub.UI.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace FunDub.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly MainService _mainService = new();
        public ObservableCollection<MaterialItem> _materials = MainViewModel.Materials;
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

        }


        private void MaterialsLB_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void MaterialsLB_DragLeave(object sender, DragEventArgs e)
        {

        }

        private void MaterialsLB_Drop(object sender, DragEventArgs e)
        {

        }








        private void IntroTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Intro";
            if (IntroTB.IsChecked == true) _mainService.OpenFileSelector(category);
            else _mainService.RemoveFileSelector(category);
        }

        private void IntroSaveTB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void VideoTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Video";
            if (VideoTB.IsChecked == true)
            {
                _mainService.OpenFileSelector(category);
            }
            else _mainService.RemoveFileSelector(category);

        }

        private void LogoTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Logo";
            if (LogoTB.IsChecked == true)
            {
                _mainService.OpenFileSelector(category); LogoSettingsGB.Visibility = Visibility.Visible;
            }
            else { _mainService.RemoveFileSelector(category); LogoSettingsGB.Visibility = Visibility.Hidden; }
        }

        private void LogoSaveTB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SubtitlesTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Subtitles";
            if (SubtitlesTB.IsChecked == true) _mainService.OpenFileSelector(category);
            else _mainService.RemoveFileSelector(category);
        }

        private void AudioTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Audio";
            if (AudioTB.IsChecked == true) _mainService.OpenFileSelector(category);
            else _mainService.RemoveFileSelector(category);
        }

        private void OutroTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Outro";
            if (OutroTB.IsChecked == true) _mainService.OpenFileSelector(category);
            else _mainService.RemoveFileSelector(category);
        }

        private void OutroSaveTB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FirstStratButton_Click(object sender, RoutedEventArgs e)
        {
            var renderingWindow = new QueueProcessingWindow
            {
                Owner = this, // Link to main window
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            renderingWindow.Show();
        }


        private void ManualRB_Checked(object sender, RoutedEventArgs e)
        {
            if (ManualRB.IsChecked == true)
            {

                LogoPositioningWindow logoPositioningWindow = new()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                logoPositioningWindow.ShowDialog();
            }
        }

        /*public class FFmpegCommandBuilder
        {
            public string Build(ProcessingJob job)
            {
                // 1. Determine Logo Position (Overlay filter coordinates)
                string position = job.LogoPosition switch
                {
                    "TopLeft" => "10:10",
                    "TopRight" => "main_w-overlay_w-10:10",
                    "BottomLeft" => "10:main_h-overlay_h-10",
                    "BottomRight" => "main_w-overlay_w-10:main_h-overlay_h-10",
                    _ => "10:10" // Default
                };

                // 2. Set Quality Presets
                string qualityArgs = job.Quality switch
                {
                    "Source" => "-c:v copy -c:a copy", // Fast, no re-encode
                    "High" => "-c:v libx264 -crf 18 -preset slow",
                    "Low" => "-c:v libx264 -crf 28 -s 854x480",
                    _ => "-c:v libx264 -crf 23 -preset medium" // Medium/Default
                };

                // 3. Construct the complex filter (Intro + Video + Logo Overlay)
                // This is a simplified version of concatenating files
                return $"-i \"{job.IntroPath}\" -i \"{job.MainVideoPath}\" -i \"{job.LogoPath}\" " +
                       $"-filter_complex \"[0:v][0:a][1:v][1:a]concat=n=2:v=1:a=1[v][a];[v][2:v]overlay={position}[out]\" " +
                       $"-map \"[out]\" -map \"[a]\" {qualityArgs} \"{job.OutputPath}\"";
            }
        }*/
    }
}
