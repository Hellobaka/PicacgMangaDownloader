using PicacgMangaDownloader.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace PicacgMangaDownloader
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is DownloadViewModel vm)
            {
                vm.Password = passwordBox.Password;
            }
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DownloadViewModel vm)
            {
                if (await vm.Login())
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MainWindow.ShowError("登录失败：用户名或密码错误");
                }
            }
        }
    }
}