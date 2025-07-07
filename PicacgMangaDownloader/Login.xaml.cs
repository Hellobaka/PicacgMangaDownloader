using PicacgMangaDownloader.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
