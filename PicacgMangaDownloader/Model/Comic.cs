using PicacgMangaDownloader.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

        public Media[] Pages { get; set; } = [];

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
                for (int i = 2; i <= pages.Length; i++)
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

        private async Task<Media[]> GetComicPages(int page, User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Token))
            {
                throw new InvalidOperationException("User must be logged in to fetch pages.");
            }
            try
            {
                var node = await Picacg.SendRequest<JsonNode>($"comics/{ComicId}/order/{EpisodeOrder}/pages?page={page}", user.Token);
                return JsonSerializer.Deserialize<Media[]>(node["pages"]["docs"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pages: {ex.Message}");
                return [];
            }
        }
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

        [JsonPropertyName("_creator")]
        public string? Creator { get; set; }

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

    public class ComicPage
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
        public Media[]? Pages { get; set; }
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
}
