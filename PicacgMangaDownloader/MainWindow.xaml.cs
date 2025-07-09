using PicacgMangaDownloader.API;
using PicacgMangaDownloader.Model;
using PicacgMangaDownloader.ViewModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace PicacgMangaDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            VM = new();
            DataContext = VM;
        }

        public DownloadViewModel VM { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("Config.json"))
            {
                try
                {
                    var json = File.ReadAllText("Config.json", Encoding.UTF8);
                    var node = JsonNode.Parse(json);
                    VM.User = JsonSerializer.Deserialize<User>(node?["User"]) ?? new();
                    VM.Comics = JsonSerializer.Deserialize<ObservableCollection<ComicWrapper>>(node?["Comics"]) ?? [];
                    VM.DownloadPath = node?["DownloadPath"]?.ToString();

                    VM.LoginType = string.IsNullOrEmpty(VM.Token) ? 0 : 1;

                    Picacg.UseProxy = node?["UseProxy"]?.GetValue<bool>() ?? false;
                    Picacg.HttpProxy = node?["HttpProxy"]?.ToString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if (!(VM.User?.IsLogin ?? false))
            {
                VM.OpenLoginCommand.Execute(null);
            }
        }

        public static void ShowInfo(string content)
        {
            HandyControl.Controls.Growl.Info(content);
        }

        public static void ShowError(string content)
        {
            HandyControl.Controls.Growl.Error(content);
        }

        public static Task<bool> ShowConfirmAsync(string content)
        {
            var tcs = new TaskCompletionSource<bool>();

            HandyControl.Controls.Growl.Ask(content, (isConfirmed) =>
            {
                tcs.SetResult(isConfirmed);
                return true;
            });

            return tcs.Task;
        }

        private void OpenProxyConfig_Click(object sender, RoutedEventArgs e)
        {
            Proxy proxy = new()
            {
                UseProxy = Picacg.UseProxy,
                ProxyUrl = Picacg.HttpProxy
            };
            if (proxy.ShowDialog() == true)
            {
                Picacg.UseProxy = proxy.UseProxy;
                Picacg.HttpProxy = proxy.ProxyUrl;

                VM.SaveConfig();
            }
        }
    }
}