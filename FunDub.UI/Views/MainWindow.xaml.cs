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
        private MainService _mainService = new();
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
            if (VideoTB.IsChecked == true) _mainService.OpenFileSelector(category);
            else _mainService.RemoveFileSelector(category);
        }

        private void LogoTB_Click(object sender, RoutedEventArgs e)
        {
            string category = "Logo";
            if (LogoTB.IsChecked == true) { _mainService.OpenFileSelector(category); LogoSettingsGB.Visibility = Visibility.Visible; }
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
            var queueWindow = new QueueProcessingWindow();
            queueWindow.Show();
        }

        
    }
}
