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
            using HttpRequestMessage request = new(new HttpMethod(method.ToUpper()), url);
            if (param != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(param), Encoding.UTF8, "application/json");
            }
            var response = await http.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResult<T>>(json)!;
            result.Check();
            return result.Data;
        }

        private static async Task<(Stream stream, long fileLength)> DownloadStream(string url, string? authorization = null)
        {
            using HttpClient http = new()
            {
                BaseAddress = new Uri(BaseUrl),
            };
            foreach (var header in GetRequestHeader(url, "Download", authorization))
            {
                if (!http.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value))
                {
                    // Handle the case where the header could not be added
                }
            }
            using HttpRequestMessage request = new(new("Download"), url);
            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadAsStreamAsync(), response.Content.Headers?.ContentRange?.Length ?? 0);
            }
            else
            {
                throw new HttpRequestException($"Failed to download stream: {response.ReasonPhrase}");
            }
        }

        public static async Task<bool> SaveStreamToFile(DownloadTask downloadTask)
        {
            if (string.IsNullOrEmpty(downloadTask.Url) || string.IsNullOrEmpty(downloadTask.FileSavePath))
            {
                throw new ArgumentException("DownloadTask must have a valid Url and FileSavePath.");
            }
            try
            {
                (Stream stream, long fileLength) = await DownloadStream(downloadTask.Url);
                using (stream)
                {
                    downloadTask.TotalBytes = fileLength;
                    downloadTask.DownloadedBytes = 0;

                    using FileStream fileStream = new(downloadTask.FileSavePath, FileMode.Create, FileAccess.Write, FileShare.None);

                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    var lastReport = DateTime.Now;

                    while (true)
                    {
                        // 超时
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        if (await Task.WhenAny(readTask, Task.Delay(downloadTask.ReadBytesTimeout)) == readTask)
                        {
                            read = readTask.Result;
                            if (read == 0) break;
                            await fileStream.WriteAsync(buffer.AsMemory(0, read));
                            totalRead += read;
                        }
                        else
                        {
                            throw new TimeoutException($"从流读取字节超时！");
                        }

                        if (fileLength > 0 && (DateTime.Now - lastReport).TotalMilliseconds > 100)
                        {
                            downloadTask.UpdateProgress(totalRead, fileLength);
                            lastReport = DateTime.Now;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving stream to file: {ex.Message}");
                return false;
            }
        }

        private static Dictionary<string, string> GetRequestHeader(string url, string method, string? authorization = null)
        {
            if (string.IsNullOrEmpty(authorization))
            {
                return [];
            }
            url = url.Replace(BaseUrl, "");
            method = method.ToUpper();

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
            long timeDiff = 0;
            string nonce = Guid.NewGuid().ToString("N");
            string apiKey = "C69BAF41DA5ABD1FFEDC6D2FEA56B";
            string appVersion = "2.2.1.3.3.4";
            string appBuildVersion = "45";

            return new()
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