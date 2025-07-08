using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace PicacgMangaDownloader.Controls
{
    /// <summary>
    /// AnimatedImage.xaml 的交互逻辑
    /// </summary>
    public partial class AnimatedImage : UserControl
    {
        private int CurrentImageIndex { get; set; } = 0;
      
        private DispatcherTimer AnimationTimer { get; set; }

        private List<BitmapImage> ImageSources { get; set; } = [];

        public AnimatedImage()
        {
            InitializeComponent();

            AnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            AnimationTimer.Tick += AnimationTimer_Tick;
            LoadGIF();
        }

        private void LoadGIF()
        {
            ImageSources = [];
            for (int i = 1; i <= 10; i++)
            {
                var uri = new Uri($"pack://application:,,,/Resources/LoadingGIF/loading_{i}.png", UriKind.Absolute);
                ImageSources.Add(new BitmapImage(uri));
            }
            StartAnimation();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (ImageSources == null || ImageSources.Count == 0 || !IsVisible)
            {
                return;
            }

            CurrentImageIndex = (CurrentImageIndex + 1) % ImageSources.Count;
            ImgPlayer.Source = ImageSources[CurrentImageIndex];
        }

        private void StartAnimation()
        {
            if (ImageSources == null || ImageSources.Count == 0)
            {
                ImgPlayer.Source = null;
                AnimationTimer.Stop();
                return;
            }

            CurrentImageIndex = 0;
            ImgPlayer.Source = ImageSources[0];
            AnimationTimer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            AnimationTimer.Stop();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            StartAnimation();
        }
    }
}
