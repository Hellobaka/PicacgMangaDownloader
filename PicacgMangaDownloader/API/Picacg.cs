using PicacgMangaDownloader.Model;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PicacgMangaDownloader.API
{
    public static class Picacg
    {
        public static string BaseUrl { get; } = "https://picaapi.picacomic.com/";

        public static async Task<T?> SendRequest<T>(string url, string? authorization = null, object param = null, string method = "GET")
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));
            }
            using HttpClient http = new()
            {
                BaseAddress = new Uri(BaseUrl),
            };
            foreach (var header in GetRequestHeader(url, method, authorization))
            {
                if (!http.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value))
                {
                    // Handle the case where the header could not be added
                }
            }
            http.DefaultRequestHeaders.Add("Connection", "keep-alive");
            using HttpRequestMessage request = new(new HttpMethod(method.ToUpper()), url);
            if (param != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(param), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json", "UTF-8"));
            }
            var response = await http.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResult<T>>(json)!;
            result.Check();
            return result.Data;
        }

        public static async Task<(Stream stream, long fileLength)> DownloadStream(string url, string? authorization = null)
        {
            using HttpClient http = new()
            {
                BaseAddress = new Uri(BaseUrl),
            };
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadAsStreamAsync(), response.Content.Headers?.ContentLength ?? 0);
            }
            else
            {
                throw new HttpRequestException($"Failed to download stream: {response.ReasonPhrase}");
            }
        }

        private static Dictionary<string, string> GetRequestHeader(string url, string method, string? authorization = null)
        {
            url = url.Replace(BaseUrl, "");
            method = method.ToUpper();

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
            long timeDiff = 0;
            string nonce = Guid.NewGuid().ToString("N");
            string apiKey = "C69BAF41DA5ABD1FFEDC6D2FEA56B";
            string appVersion = "2.2.1.3.3.4";
            string appBuildVersion = "45";

            var header = new Dictionary<string, string>()
            {
                {"signature", GetSignature(url, method, BaseUrl, timestamp, timeDiff, nonce, apiKey, appVersion, appBuildVersion) },
                {"api-key", apiKey },
                {"app-channel", "3" },
                {"time", timestamp.ToString() },
                {"nonce", nonce },
                {"app-version", appVersion },
                {"app-build-version", appBuildVersion },
                {"app-uuid", "defaultUuid" },
                {"image-quality", "original" },
                {"app-platform", "android" },
                {"user-agent", "okhttp/3.8.1" },
                {"version", "v1.5.2" },
                {"accept", "application/vnd.picacomic.com.v1+json" },
            };
            if (!string.IsNullOrEmpty(authorization))
            {
                header.Add("authorization", authorization);
            }
            if (method == "POST")
            {
                header.Add("Content-Type", "application/json; charset=UTF-8");
            }
            return header;
        }

        private static string GetSignature(string url, string method, string baseUrl, long timestamp, long timeDiff, string nonce, string apiKey, string appVersion, string appBuildVersion)
        {
            string[] param =
            [
                baseUrl,
                url,
                (timestamp + timeDiff).ToString(),
                nonce,
                method,
                apiKey,
                appVersion,
                appBuildVersion,
            ];

            string connectString = GetConnectString(param);
            byte[] bytes = Encoding.UTF8.GetBytes(connectString.ToLower());
            byte[] hmac = HmacSha256(bytes, Encoding.UTF8.GetBytes(GetSigString()));
            return Convert.ToHexString(hmac).ToLower();
        }

        private static string GetSigString()
        {
            return "~d}$Q7$eIni=V)9\\RK/P.RM4;9[7|@/CA}b~OW!3?EV`:<>M7pddUBL5n|0/*Cn";
        }

        private static string GetConnectString(string[] param)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(param[1]);
            stringBuilder.Append(param[2]);
            stringBuilder.Append(param[3]);
            stringBuilder.Append(param[4]);
            stringBuilder.Append(param[5]);
            return stringBuilder.ToString();
        }

        private static byte[] HmacSha256(byte[] bytes, byte[] key)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(key);
            return hmac.ComputeHash(bytes);
        }
    }
}