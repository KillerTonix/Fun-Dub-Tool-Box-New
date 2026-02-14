using FunDub.UI.Converters;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FunDub.UI.Views
{
    /// <summary>
    /// Interaction logic for LogoPositioningWindow.xaml
    /// </summary>
    public partial class LogoPositioningWindow : Window
    {
        private static readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        private static readonly List<string> temproraryImagesPath = [];
        private bool _isDragging = false;
        private Point _clickPosition;


        public LogoPositioningWindow()
        {
            InitializeComponent();

            SetBackgroundImage(); // Load background image from video preview
            SetLogoImage(); // Load logo image from materials
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(OverlayCanvas);

                // Adjust for where you clicked inside the logo so it doesn't "jump" to center
                double x = p.X - _clickPosition.X;
                double y = p.Y - _clickPosition.Y;

                // Bounds checking
                x = Math.Clamp(x, 0, OverlayCanvas.ActualWidth - LogoImageBorder.ActualWidth);
                y = Math.Clamp(y, 0, OverlayCanvas.ActualHeight - LogoImageBorder.ActualHeight);

                Canvas.SetLeft(LogoImageBorder, x);
                Canvas.SetTop(LogoImageBorder, y);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();          
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            double x = Canvas.GetLeft(LogoImageBorder);
            double y = Canvas.GetTop(LogoImageBorder);

            // Calculate relative position (0.0 to 1.0)
            double relativeX = x / OverlayCanvas.ActualWidth;
            double relativeY = y / OverlayCanvas.ActualHeight;
            double finalScale = ScaleSlider.Value;
            double finalOpacity = OpacitySlider.Value;

            // You can now pass these 4 values back to your FFmpeg command builder!
            this.DialogResult = true;
        }

        private void UpdateBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            SetBackgroundImage();
        }

        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LogoImageBorder == null) return;

            // We apply a ScaleTransform to the Border
            ScaleTransform scale = new(e.NewValue, e.NewValue);
            LogoImageBorder.RenderTransform = scale;

            // Optional: Keep the scaling centered
            LogoImageBorder.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LogoImageBorder == null) return;

            // Simple Opacity change
            LogoImageBorder.Opacity = e.NewValue;
        }

        private async void SetBackgroundImage()
        {
            string _sourceVideoPath = mainWindow._materials.FirstOrDefault(m => m.Type == "Video")?.Path ?? string.Empty;
            string _backgroundImageForLogoPath = await VideoToImage.LoadVideoPreviewAsync(_sourceVideoPath);
            if (!string.IsNullOrEmpty(_backgroundImageForLogoPath))
            {
                temproraryImagesPath.Add(_backgroundImageForLogoPath);
                VideoPreviewImage.Source = new BitmapImage(new Uri(_backgroundImageForLogoPath));
            }
        }

        private void SetLogoImage()
        {
            string _logoImagePath = mainWindow._materials.FirstOrDefault(m => m.Type == "Logo")?.Path ?? string.Empty;
            BitmapImage logoBitmap = new();
            logoBitmap.BeginInit();
            logoBitmap.UriSource = new Uri(_logoImagePath);
            logoBitmap.EndInit();
            LogoImageBorder.Background = new ImageBrush(logoBitmap);
            LogoImageBorder.Width = logoBitmap.PixelWidth;
            LogoImageBorder.Height = logoBitmap.PixelHeight;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (string path in temproraryImagesPath)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error deleting temporary file: {ex.Message}");
                }
            }
        }

        private void LogoImageBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _clickPosition = e.GetPosition(LogoImageBorder);
            LogoImageBorder.CaptureMouse(); // Keeps the mouse locked to the logo while dragging
        }

        private void LogoImageBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            LogoImageBorder.ReleaseMouseCapture();
        }
    }
}
