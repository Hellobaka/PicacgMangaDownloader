using System.Text.Json.Serialization;

namespace PicacgMangaDownloader.API
{
    public class ApiResult<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("error")]
        public int? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        public void Check()
        {
            if (Error.HasValue && Error.Value != 0)
            {
                throw new Exception($"API Error: {Error.Value} - {Message}");
            }
        }
    }
}