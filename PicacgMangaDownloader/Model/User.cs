using PicacgMangaDownloader.API;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PicacgMangaDownloader.Model
{
    public class User
    {
        [JsonPropertyName("_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("name")]
        public string? UserName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("birthday")]
        public DateTime? Birthday { get; set; }

        [JsonPropertyName("activation_date")]
        public DateTime? ActivationDate { get; set; }

        [JsonPropertyName("last_login_date")]
        public DateTime? LastLoginDate { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("slogan")]
        public string? Slogan { get; set; }

        [JsonPropertyName("exp")]
        public int? Exp { get; set; }

        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("isPunched")]
        public bool? IsPunched { get; set; }

        public string? Token { get; set; }

        public bool IsLogin => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token);

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>Token</returns>
        public async Task<bool> Login(string userName, string password)
        {
            try
            {
                var node = await Picacg.SendRequest<JsonNode>("auth/sign-in", param: new { email = userName, password }, method: "POST");
                Token = node["token"]?.ToString();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProfile()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("User is not logged in.");
                return false;
            }
            try
            {
                var node = await Picacg.SendRequest<JsonNode>("users/profile", Token);
                var user = JsonSerializer.Deserialize<User>(node["user"]);
                this.UserId = user.UserId;
                this.UserName = user.UserName;
                this.Email = user.Email;
                this.Gender = user.Gender;
                this.Birthday = user.Birthday;
                this.ActivationDate = user.ActivationDate;
                this.LastLoginDate = user.LastLoginDate;
                this.Title = user.Title;
                this.Slogan = user.Slogan;
                this.Exp = user.Exp;
                this.Level = user.Level;
                this.IsPunched = user.IsPunched;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ComicInfo[]> GetFavoriteComics(int page = -1)
        {
            if (page == -1)
            {
                // 获取所有
                List<ComicInfo> comicInfos = [];
                var list = await GetComicListAsync(1);
                comicInfos.AddRange(list.Comics ?? []);
                for (int i = 2; i <= list.TotalPage; i++)
                {
                    var nextList = await GetComicListAsync(i);
                    comicInfos.AddRange(nextList.Comics ?? []);
                }
                return comicInfos.Reverse<ComicInfo>().ToArray();
            }
            else
            {
                // 获取指定页
                var list = await GetComicListAsync(page);
                return list.Comics ?? [];
            }
        }

        private async Task<ComicList> GetComicListAsync(int page)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("User is not logged in.");
            }
            try
            {
                var node = await Picacg.SendRequest<JsonNode>($"users/favourite?page={page}", Token);
                var comicList = JsonSerializer.Deserialize<ComicList>(node["comics"]);
                return comicList;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get favorite comics.", e);
            }
        }
    }

    public class Avatar
    {
        [JsonPropertyName("originalName")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("fileServer")]
        public string? FileServer { get; set; }
    }
}
