using PicacgMangaDownloader.API;
using PicacgMangaDownloader.ViewModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PicacgMangaDownloader.Model
{
    public class ComicList
    {
        [JsonPropertyName("page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("pages")]
        public int TotalPage { get; set; }

        [JsonPropertyName("total")]
        public int TotalCount { get; set; }

        [JsonPropertyName("limit")]
        public int PageLimit { get; set; }

        [JsonPropertyName("docs")]
        public ComicInfo[]? Comics { get; set; }
    }

    public class ComicEpisodeInfo
    {
        [JsonPropertyName("page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("total")]
        public int TotalPage { get; set; }

        [JsonPropertyName("pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("limit")]
        public int PageLimit { get; set; }

        [JsonPropertyName("docs")]
        public ComicEpisode[]? Eps { get; set; }
    }

    public class ComicEpisode
    {
        [JsonPropertyName("_id")]
        public string? EpisodeId { get; set; }

        [JsonPropertyName("title")]
        public string? EpisodeTitle { get; set; }

        [JsonPropertyName("order")]
        public int? EpisodeOrder { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public string ComicId { get; set; } = "";

        public int TotalPage { get; set; }

        public ComicPage[] Pages { get; set; } = [];

        public async Task GetPages(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Token))
            {
                throw new InvalidOperationException("User must be logged in to fetch pages.");
            }
            if (string.IsNullOrEmpty(EpisodeId))
            {
                throw new InvalidOperationException("EpisodeId must be set before fetching pages.");
            }
            try
            {
                var pages = await GetComicPages(1, user);
                if (pages == null || pages.Length == 0)
                {
                    Console.WriteLine("No pages found for this episode.");
                    return;
                }
                Pages = [.. Pages, .. pages];
                for (int i = 2; i <= TotalPage; i++)
                {
                    var nextPages = await GetComicPages(i, user);
                    if (nextPages == null || nextPages.Length == 0)
                    {
                        Console.WriteLine($"No more pages found on page {i}.");
                        break;
                    }
                    Pages = [.. Pages, .. nextPages];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pages: {ex.Message}");
            }
        }

        private async Task<ComicPage[]> GetComicPages(int page, User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Token))
            {
                throw new InvalidOperationException("User must be logged in to fetch pages.");
            }
            try
            {
                var node = await Picacg.SendRequest<JsonNode>($"comics/{ComicId}/order/{EpisodeOrder}/pages?page={page}", user.Token);
                TotalPage = Math.Max(TotalPage, (int)node["pages"]["pages"]);
                return JsonSerializer.Deserialize<ComicPage[]>(node["pages"]["docs"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pages: {ex.Message}");
                return [];
            }
        }
    }

    public class ComicPage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("media")]
        public Media Media { get; set; }
    }

    public class ComicInfo
    {
        [JsonPropertyName("_id")]
        public string? ComicId { get; set; }

        [JsonPropertyName("title")]
        public string? ComicTitle { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("pagesCount")]
        public int? PagesCount { get; set; }

        [JsonPropertyName("epsCount")]
        public int? EPCount { get; set; }

        [JsonPropertyName("categories")]
        public string[]? Categories { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }

        [JsonPropertyName("likesCount")]
        public int? LikesCount { get; set; }

        //[JsonPropertyName("_creator")]
        //public string? Creator { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("chineseTeam")]
        public string? ChineseTeam { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("viewsCount")]
        public int? ViewsCount { get; set; }

        [JsonPropertyName("commentsCount")]
        public int? CommentsCount { get; set; }

        [JsonPropertyName("isFavourite")]
        public bool? Favorited { get; set; }

        [JsonPropertyName("isLiked")]
        public bool? Liked { get; set; }

        [JsonPropertyName("thumb")]
        public Media? Thumb { get; set; }

        public ComicEpisode[] Episodes { get; set; } = [];

        public async Task GetEpisodes(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Token))
            {
                throw new InvalidOperationException("User must be logged in to fetch episodes.");
            }
            if (string.IsNullOrEmpty(ComicId))
            {
                throw new InvalidOperationException("ComicId must be set before fetching episodes.");
            }
            try
            {
                var episode = await GetComicEpisodes(1, user.Token);
                if (episode == null || episode.Eps == null || episode.Eps.Length == 0)
                {
                    Console.WriteLine("No episodes found for this comic.");
                    return;
                }
                Episodes = [.. Episodes, .. episode.Eps];
                for (int i = 2; i <= episode.TotalPage; i++)
                {
                    var nextList = await GetComicEpisodes(i, user.Token);
                    if (nextList == null || nextList.Eps == null || nextList.Eps.Length == 0)
                    {
                        Console.WriteLine($"No more episodes found on page {i}.");
                        break;
                    }
                    Episodes = [.. Episodes, .. nextList.Eps];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching episodes: {ex.Message}");
            }
        }

        private async Task<ComicEpisodeInfo?> GetComicEpisodes(int page, string authorization)
        {
            var node = await Picacg.SendRequest<JsonNode>($"comics/{ComicId}/eps?page={page}", authorization);
            var episodeInfo = JsonSerializer.Deserialize<ComicEpisodeInfo>(node["eps"]);
            if (episodeInfo != null && episodeInfo.Eps != null && episodeInfo.Eps.Length > 0)
            {
                return episodeInfo;
            }

            return null;
        }

        public async Task GetMoreInformation(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Token) || string.IsNullOrEmpty(ComicId))
            {
                throw new InvalidOperationException("User must be logged in and ComicId must be set.");
            }
            try
            {
                var node = await Picacg.SendRequest<JsonNode>($"comics/{ComicId}", user.Token);
                var comicInfo = JsonSerializer.Deserialize<ComicInfo>(node["comic"]);
                if (comicInfo != null)
                {
                    this.Author = comicInfo.Author;
                    this.Tags = comicInfo.Tags;
                    this.Description = comicInfo.Description;
                    this.ChineseTeam = comicInfo.ChineseTeam;
                    this.UpdatedAt = comicInfo.UpdatedAt;
                    this.CreatedAt = comicInfo.CreatedAt;
                    this.ViewsCount = comicInfo.ViewsCount;
                    this.CommentsCount = comicInfo.CommentsCount;
                    this.Favorited = comicInfo.Favorited;
                    this.Liked = comicInfo.Liked;
                    this.Thumb = comicInfo.Thumb;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching more information: {ex.Message}");
            }
        }
    }

    public class Media
    {
        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("fileServer")]
        public string? FileServer { get; set; }

        public string GetFullUrl()
        {
            if (string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(FileServer))
            {
                return string.Empty;
            }
            if (FileServer.EndsWith("static/") || FileServer.EndsWith("static"))
            {
                // 如果 FileServer 以 "static/" 或 "static" 结尾，则不需要添加路径前缀
                return $"{FileServer.TrimEnd('/')}/{Path.TrimStart('/')}";
            }

            return $"{FileServer.TrimEnd('/')}/static/{Path.TrimStart('/')}";
        }
    }

    public enum DownloadStatus
    {
        NotDownloaded,
        Downloading,
        Downloaded,
        DownloadFailed
    }

    [AddINotifyPropertyChangedInterface]
    public class ComicWrapper
    {
        public ComicInfo? Comic { get; set; }

        [AlsoNotifyFor("EpisodeFinishedCount", "EpisodeTotalCount", "Percentage")]
        public ObservableCollection<ComicEpisodeWrapper> Episodes { get; set; } = [];

        public bool Selected { get; set; }

        public DownloadStatus Downloading { get; set; } = Model.DownloadStatus.NotDownloaded;

        public string DownloadStatus => Downloading switch
        {
            Model.DownloadStatus.Downloading => "下载中",
            Model.DownloadStatus.Downloaded => "已下载",
            Model.DownloadStatus.DownloadFailed => "下载失败",
            _ => "未开始",
        };

        public int EpisodeFinishedCount => Episodes.Count(x => x.TotalCount == (x.Failed + x.DownloadedCount));

        public int EpisodeTotalCount => Episodes.Count;

        public double Percentage => EpisodeTotalCount > 0 ? (double)EpisodeFinishedCount / EpisodeTotalCount * 100 : 0.0;

        public async Task GetEpisodes(User user)
        {
            if (Episodes.Count == 0)
            {
                await Comic.GetEpisodes(user);
                Episodes = [];
                foreach (var ep in Comic.Episodes)
                {
                    ep.ComicId = Comic.ComicId;
                    var epWrapper = new ComicEpisodeWrapper()
                    {
                        ComicTitle = Comic.ComicTitle,
                        Episode = ep,
                        Pages = [],
                    };
                    await epWrapper.Episode.GetPages(user);
                    foreach (var page in epWrapper.Episode.Pages)
                    {
                        var pageWrapper = new ComicPageWrapper()
                        {
                            Page = page,
                        };
                        epWrapper.Pages.Add(pageWrapper);
                    }

                    epWrapper.Subscribe();
                    Episodes.Add(epWrapper);
                }
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ComicEpisodeWrapper
    {
        public string ComicTitle { get; set; } = string.Empty;

        public ComicEpisode? Episode { get; set; }

        [AlsoNotifyFor("Failed", "DownloadedCount", "TotalCount", "Percentage")]
        public ObservableCollection<ComicPageWrapper> Pages { get; set; } = [];

        public bool Selected { get; set; }

        public int Failed => Pages.Count(x => x.Downloading == Model.DownloadStatus.DownloadFailed);

        public int DownloadedCount => Pages.Count(x => x.Downloading == Model.DownloadStatus.Downloaded);

        public int TotalCount => Pages.Count;

        public double Percentage => TotalCount > 0 ? (double)(DownloadedCount + Failed) / TotalCount * 100 : 0.0;

        public DownloadStatus Downloading { get; set; } = Model.DownloadStatus.NotDownloaded;

        public string DownloadStatus => Downloading switch
        {
            Model.DownloadStatus.Downloading => "下载中",
            Model.DownloadStatus.Downloaded => "已下载",
            Model.DownloadStatus.DownloadFailed => "下载失败",
            _ => "未开始",
        };

        private CancellationTokenSource? DownloadCancelToken { get; set; }

        /// <summary>
        /// 启动本章节所有页面的下载任务，支持最大并发数限制。
        /// </summary>
        public async Task StartDownload()
        {
            DownloadCancelToken?.Cancel();
            DownloadCancelToken = new CancellationTokenSource();
            var token = DownloadCancelToken.Token;
            this.Downloading = Model.DownloadStatus.Downloading;

            string episodeFolder = Path.Combine(
                DownloadViewModel.Instance.DownloadPath ?? string.Empty,
                ComicTitle ?? string.Empty,
                DownloadViewModel.Instance.KeepEpisodeTitle ? (Episode?.EpisodeTitle ?? string.Empty) : (Episode?.EpisodeOrder?.ToString() ?? string.Empty));

            var throttler = new SemaphoreSlim(DownloadViewModel.Instance.MaxParallelDownloads);
            var tasks = Pages.Select(async page =>
            {
                await throttler.WaitAsync(token);
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    page.CreateDownloadTask(episodeFolder);
                    if (page.DownloadTask is DownloadTask task)
                    {
                        try
                        {
                            await task.StartAsync(token);
                        }
                        catch (OperationCanceledException)
                        {
                            // 取消时忽略异常
                        }
                    }
                }
                finally
                {
                    throttler.Release();
                }
            });
            try
            {
                await Task.WhenAll(tasks);
            }
            finally
            {
                this.Downloading = Pages.All(p => p.Downloading == Model.DownloadStatus.Downloaded) ? Model.DownloadStatus.Downloaded :
                                   Pages.Any(p => p.Downloading == Model.DownloadStatus.DownloadFailed) ? Model.DownloadStatus.DownloadFailed :
                                   Model.DownloadStatus.NotDownloaded;
            }
        }

        /// <summary>
        /// 停止本章节所有页面的下载任务。
        /// </summary>
        public async Task StopDownload()
        {
            DownloadCancelToken?.Cancel();
            foreach (var page in Pages)
            {
                page.CancelDownload();
            }
            this.Downloading = Model.DownloadStatus.NotDownloaded;
            await Task.CompletedTask;
        }


        public void Subscribe()
        {
            foreach (var page in Pages)
            {
                page.SubscribeDownloadProgress();
            }
        }

        public void Unsubscribe()
        {
            foreach (var page in Pages)
            {
                page.UnsubscribeDownloadProgress();
            }
        }
    }

    [AddINotifyPropertyChangedInterface]
    public class ComicPageWrapper
    {
        public ComicPage? Page { get; set; }

        public DownloadTask? DownloadTask { get; set; }

        public DownloadStatus Downloading { get; set; } = Model.DownloadStatus.NotDownloaded;

        public void CreateDownloadTask(string parentDirectory)
        {
            if (Page == null) return;
            var media = Page.Media;
            string fileName = media.OriginalName ?? Path.GetFileName(media.Path ?? Guid.NewGuid().ToString());
            string filePath = Path.Combine(parentDirectory, fileName);
            DownloadTask = new DownloadTask
            {
                FileSavePath = filePath,
                Url = media.GetFullUrl(),
            };
            Downloading = Model.DownloadStatus.Downloading;
            SubscribeDownloadProgress();
        }

        public void CancelDownload()
        {
            Downloading = Model.DownloadStatus.NotDownloaded;
            DownloadTask?.Cancel();
            UnsubscribeDownloadProgress();
        }


        public string DownloadStatus => Downloading switch
        {
            Model.DownloadStatus.Downloading => "下载中",
            Model.DownloadStatus.Downloaded => "已下载",
            Model.DownloadStatus.DownloadFailed => "下载失败",
            _ => "未开始",
        };

        public void SubscribeDownloadProgress()
        {
            if (DownloadTask == null)
            {
                return;
            }
            DownloadTask.OnCompleted -= DownloadTask_OnCompleted;
            DownloadTask.OnFailed -= DownloadTask_OnFailed;
            DownloadTask.OnCompleted += DownloadTask_OnCompleted;
            DownloadTask.OnFailed += DownloadTask_OnFailed;
            //DownloadTask.OnDownloadProgressUpdated += DownloadTask_OnDownloadProgressUpdated;
        }

        public void UnsubscribeDownloadProgress()
        {
            if (DownloadTask == null)
            {
                return;
            }
            DownloadTask.OnCompleted -= DownloadTask_OnCompleted;
            DownloadTask.OnFailed -= DownloadTask_OnFailed;
        }

        private void DownloadTask_OnDownloadProgressUpdated(DownloadTask arg1, long arg2, long arg3, double arg4)
        {
        }

        private void DownloadTask_OnFailed(DownloadTask downloadTask)
        {
            Downloading = Model.DownloadStatus.DownloadFailed;
        }

        private void DownloadTask_OnCompleted(DownloadTask downloadTask)
        {
            Downloading = Model.DownloadStatus.Downloaded;
        }
    }
}
