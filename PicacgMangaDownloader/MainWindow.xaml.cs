using PicacgMangaDownloader.ViewModel;
using System.IO;
using System.Text;
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
            DataContext = this;
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
                    VM.Token = node?["PicacgToken"]?.ToString();

                    VM.LoginType = string.IsNullOrEmpty(VM.Token) ? 0 : 1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Login login = new();
            login.DataContext = VM;
            login.Owner = this;
            login.ShowDialog();
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
    }
}