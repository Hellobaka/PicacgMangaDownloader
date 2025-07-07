using PicacgMangaDownloader.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PicacgMangaDownloader.ViewModel
{
    public class DownloadViewModel : INotifyPropertyChanged
    {
        public DownloadViewModel()
        {
            BrowserOutputPathCommand = new((_) => BrowserOutputPath());
            GetFavoriteComicsCommand = new((_) => GetFavoriteComics());
            OpenLoginCommand = new((_) => OpenLogin());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Token { get; set; }

        public User? User { get; set; }

        public string DisplayedUserName => string.IsNullOrEmpty(User?.UserName) ? "未登录" : User.UserName;

        public bool IsLogin => User?.IsLogin ?? false;

        public bool KeepEpisodeTitle { get; set; }

        public bool DryRun { get; set; }

        public bool Logining { get; set; } = false;

        public bool Downloading { get; set; } = false;

        public int LoginType { get; set; }

        public int ComicTotalCount => Comics.Count;

        public int ComicFinishedCount => Comics.Count(x => x.EpisodeFinishedCount == x.EpisodeTotalCount);

        public double Percentage => ComicTotalCount == 0 ? 0 : (double)ComicFinishedCount / ComicTotalCount * 100;
      
        public ObservableCollection<ComicWrapper> Comics { get; set; } = [];

        public RelayCommand LoginByPasswordCommand { get; set; }

        public RelayCommand LoginByTokenCommand { get; set; }

        public RelayCommand BrowserOutputPathCommand { get; set; }

        public RelayCommand GetFavoriteComicsCommand { get; set; }

        public RelayCommand OpenLoginCommand { get; set; }

        private protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenLogin()
        {
            throw new NotImplementedException();
        }

        private void GetFavoriteComics()
        {
            throw new NotImplementedException();
        }

        private void BrowserOutputPath()
        {
            throw new NotImplementedException();
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

        public async Task<bool> Login()
        {
            try
            {
                Logining = true;
                return LoginType switch
                {
                    1 => await LoginByToken(),
                    _ => await LoginByPassword(),
                };
            }
            catch { }
            finally
            {
                Logining = false;
            }

            return false;
        }
    }
}
