using Microsoft.Win32;
using PicacgMangaDownloader.Model;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace PicacgMangaDownloader.ViewModel
{
    public class DownloadViewModel : INotifyPropertyChanged
    {
        public DownloadViewModel()
        {
            BrowserOutputPathCommand = new RelayCommand(_ => BrowserOutputPath());
            GetFavoriteComicsCommand = new RelayCommand(async _ => await GetFavoriteComics());
            OpenLoginCommand = new RelayCommand(_ => OpenLogin());
            StartDownloadCommand = new RelayCommand(_ => StartDownload());
            CancelDownloadCommand = new RelayCommand(_ => CancelDownload());
            ComicSelectAllCommand = new RelayCommand(_ => ComicSelectAll());
            ComicSelectRevertCommand = new RelayCommand(_ => ComicSelectRevert());
            Instance = this;
        }
        public RelayCommand CancelDownloadCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Token { get; set; }

        public string? DownloadPath { get; set; }

        [AlsoNotifyFor("IsLogin", "DisplayedUserName")]
        public User? User { get; set; }

        public string DisplayedUserName => string.IsNullOrEmpty(User?.UserName) ? "未登录" : User.UserName;

        public bool IsLogin => User?.IsLogin ?? false;

        public bool KeepEpisodeTitle { get; set; }

        public bool DryRun { get; set; }

        public bool Logining { get; set; } = false;

        public bool Downloading { get; set; } = false;

        public int LoginType { get; set; }

        public int MaxParallelDownloads { get; set; } = 6;

        public int ComicTotalCount => Comics.Count;

        public int ComicFinishedCount => Comics.Count(x => x.EpisodeTotalCount != 0 && x.EpisodeFinishedCount == x.EpisodeTotalCount);

        public double Percentage => ComicTotalCount == 0 ? 0 : (double)ComicFinishedCount / ComicTotalCount * 100;

        public ObservableCollection<ComicWrapper> Comics { get; set; } = [];

        public RelayCommand BrowserOutputPathCommand { get; set; }

        public RelayCommand GetFavoriteComicsCommand { get; set; }

        public RelayCommand StartDownloadCommand { get; set; }

        public RelayCommand OpenLoginCommand { get; set; }

        public RelayCommand ComicSelectAllCommand { get; set; }

        public RelayCommand ComicSelectRevertCommand { get; set; }

        public static DownloadViewModel Instance { get; set; }

        private protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenLogin()
        {
            Login login = new();
            login.DataContext = this;
            login.ShowDialog();
        }

        private async Task GetFavoriteComics()
        {
            var comics = await User.GetFavoriteComics();
            foreach (var item in Comics.SelectMany(x => x.Episodes))
            {
                item.Unsubscribe();
            }
            Comics = [];
            foreach (var comic in comics)
            {
                Comics.Add(new ComicWrapper
                {
                    Comic = comic,
                    Episodes = [],
                    Selected = false
                });
            }
            SaveConfig();
        }

        private void BrowserOutputPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择下载漫画要保存的位置",
            };
            if (dialog.ShowDialog() ?? false)
            {
                DownloadPath = dialog.FolderName;
                SaveConfig();
            }
        }

        private async void StartDownload()
        {
            if (string.IsNullOrEmpty(DownloadPath))
            {
                MainWindow.ShowError("未指定下载路径，请重新选择");
                return;
            }
            Directory.CreateDirectory(DownloadPath);
            foreach (var comic in Comics.Where(x => x.Selected))
            {
                // await comic.Comic.GetMoreInformation(User);
                await comic.GetEpisodes(User);
                if (comic.Episodes.Count == 0)
                {
                    MainWindow.ShowError($"漫画 {comic.Comic.ComicTitle} 没有可下载的章节");
                    continue;
                }
                // 启动所有章节下载（可并发）
                var tasks = comic.Episodes.Select(ep => ep.StartDownload());
                await Task.WhenAll(tasks);
            }
        }
        
        // 取消所有正在下载的章节
        private void CancelDownload()
        {
            foreach (var comic in Comics.Where(x => x.Selected))
            {
                foreach (var ep in comic.Episodes)
                {
                    _ = ep.StopDownload();
                }
            }
        }

        private async Task<bool> LoginByToken()
        {
            User = new()
            {
                Token = Token
            };
            return await User.UpdateProfile();
        }

        private async Task<bool> LoginByPassword()
        {
            User = new();
            return await User.Login(Username, Password) && await User.UpdateProfile();
        }

        private void ComicSelectRevert()
        {
            foreach (var item in Comics)
            {
                item.Selected = !item.Selected;
            }
        }

        private void ComicSelectAll()
        {
            foreach (var item in Comics)
            {
                item.Selected = true;
            }
        }

        public async Task<bool> Login()
        {
            try
            {
                Logining = true;
                var result = LoginType switch
                {
                    1 => await LoginByToken(),
                    _ => await LoginByPassword(),
                };
                if (result)
                {
                    OnPropertyChanged(nameof(IsLogin));
                    OnPropertyChanged(nameof(DisplayedUserName));
                    OnPropertyChanged(nameof(User));

                    SaveConfig();
                }
                return result;
            }
            catch { }
            finally
            {
                Logining = false;
            }

            return false;
        }

        private void SaveConfig()
        {
            File.WriteAllText("Config.json", JsonSerializer.Serialize(new
            {
                Comics,
                User,
                DownloadPath
            }), System.Text.Encoding.UTF8);
        }
    }
}
