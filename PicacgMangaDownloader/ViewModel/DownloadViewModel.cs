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
            GetComicsEpisodeCommand = new RelayCommand(async (comic) => await GetComicsEpisode(comic));
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

        public string DisplayedUserName => string.IsNullOrEmpty(User?.UserName) ? "未登录" : $"{User.UserName} Lv.{User.Level} Exp: {User.Exp}";

        public bool IsLogin => User?.IsLogin ?? false;

        public bool KeepEpisodeTitle { get; set; }

        public bool DryRun { get; set; }

        public bool Logining { get; set; } = false;

        public bool Downloading { get; set; }

        public int LoginType { get; set; }

        public bool GettingFavoriteComics { get; set; }

        public int MaxParallelDownloads { get; set; } = 50;

        public SemaphoreSlim DownloadThrottler { get; set; }

        public int ComicTotalCount { get; set; }

        public int ComicFinishedCount { get; set; }

        public double Percentage => ComicTotalCount == 0 ? 0 : (double)ComicFinishedCount / ComicTotalCount * 100;

        public ObservableCollection<ComicWrapper> Comics { get; set; } = [];

        public RelayCommand BrowserOutputPathCommand { get; set; }

        public RelayCommand GetComicsEpisodeCommand { get; set; }

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
            try
            {
                GettingFavoriteComics = true;
                if (User == null || !IsLogin)
                {
                    MainWindow.ShowError("请先登录账号");
                    return;
                }
                if (Downloading)
                {
                    MainWindow.ShowError("下载过程不可重新获取收藏列表");
                    return;
                }
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
            catch (Exception e)
            {
                MainWindow.ShowError($"获取收藏列表时发生错误：{e.Message}");
            }
            finally
            {
                GettingFavoriteComics = false;
            }
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
            if (Comics.Count == 0)
            {
                MainWindow.ShowError("没有可下载的漫画，请先获取漫画列表");
                return;
            }
            if (Comics.All(x => !x.Selected))
            {
                MainWindow.ShowError("没有选择任何漫画，请先选择要下载的漫画");
                return;
            }
            if (User == null || !IsLogin)
            {
                MainWindow.ShowError("请先登录账号");
                return;
            }
            DownloadThrottler?.Dispose();

            DownloadThrottler = new(MaxParallelDownloads);
            Directory.CreateDirectory(DownloadPath);
            int index = 1;
            try
            {
                Downloading = true;
                var selectedComics = Comics.Where(x => x.Selected).ToList();
                ComicTotalCount = selectedComics.Count;
                ComicFinishedCount = 0;
                foreach (var comic in selectedComics)
                {
                    if (comic.Comic == null)
                    {
                        MainWindow.ShowError($"漫画索引 {index} 信息不完整，无法下载");
                        continue;
                    }

                    // await comic.Comic.GetMoreInformation(User);
                    if (comic.Episodes.Count == 0)
                    {
                        await comic.GetEpisodes(User, false);
                        if (comic.Episodes.Count == 0)
                        {
                            MainWindow.ShowError($"漫画 {comic.Comic.ComicTitle} 没有可下载的章节");
                            continue;
                        }
                    }
                    // 启动所有章节下载
                    var tasks = comic.Episodes.Where(x => x.Selected).Select(ep => ep.StartDownload());
                    OnPropertyChanged(nameof(Downloading));
                    await Task.WhenAll(tasks);

                    index++;
                    ComicFinishedCount++;
                    if (comic.Downloading == DownloadStatus.Downloaded)
                    {
                        comic.Selected = false;
                    }
                    OnPropertyChanged(nameof(Downloading));
                    OnPropertyChanged(nameof(ComicTotalCount));
                    OnPropertyChanged(nameof(ComicFinishedCount));
                    OnPropertyChanged(nameof(Percentage));
                }
            }
            catch (Exception e)
            {
                MainWindow.ShowError($"下载过程中发生错误：{e.Message}");
            }
            finally
            {
                Downloading = false;
            }
        }

        /// <summary>
        /// 取消所有正在下载的章节
        /// </summary>
        private void CancelDownload()
        {
            foreach (var comic in Comics)
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

        private async Task GetComicsEpisode(object? obj)
        {
            if (obj == null || obj is not ComicWrapper comic)
            {
                return;
            }
            if (User == null || !IsLogin)
            {
                MainWindow.ShowError("请先登录账号");
                return;
            }
            await comic.GetEpisodes(User, true);
            if (comic.GettingEpisodeHasError)
            {
                MainWindow.ShowError($"获取漫画 {comic.Comic?.ComicTitle} 章节失败，请稍后重试");
            }
            else
            {
                MainWindow.ShowInfo($"获取漫画 {comic.Comic?.ComicTitle} 章节列表成功，共 {comic.Episodes.Count} 章");
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
