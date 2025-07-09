using System.Windows;

namespace PicacgMangaDownloader
{
    /// <summary>
    /// Proxy.xaml 的交互逻辑
    /// </summary>
    public partial class Proxy : Window
    {
        public Proxy()
        {
            InitializeComponent();
        }

        public bool UseProxy { get; set; }
       
        public string ProxyUrl { get; set; }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            UseProxy = UseProxySelector.IsChecked ?? false;
            ProxyUrl = HttpProxyUrl.Text.Trim();
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UseProxySelector.IsChecked = UseProxy;
            HttpProxyUrl.Text = ProxyUrl;
        }
    }
}
