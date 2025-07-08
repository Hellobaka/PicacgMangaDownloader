using PicacgMangaDownloader.API;
using System.IO;

namespace PicacgMangaDownloader.Model
{
    public class DownloadTask
    {
        public event Action<DownloadTask>? OnCompleted;
        public event Action<DownloadTask>? OnFailed;
        public event Action<DownloadTask, long, long, double>? OnDownloadProgressUpdated;

        public string? Url { get; set; }

        public string? FileSavePath { get; set; }

        public TimeSpan ReadBytesTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public long TotalBytes { get; set; }

        public long DownloadedBytes { get; set; }

        public double Percentage => TotalBytes == 0 ? 0 : (double)DownloadedBytes / TotalBytes * 100;

        public CancellationTokenSource CancellationToken { get; set; } = new();

        public Task? _DownloadTask { get; set; }

        public bool IsActive => _DownloadTask != null && !_DownloadTask.IsCompleted;

        public async Task StartAsync(CancellationToken token)
        {
            await DownloadStreamToFile(token);
        }

        public void Cancel()
        {
            CancellationToken?.Cancel();
        }

        private async Task DownloadStreamToFile(CancellationToken token)
        {
            if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(FileSavePath))
            {
                throw new ArgumentException("DownloadTask must have a valid Url and FileSavePath.");
            }
            try
            {
                // TODO: Auto Retry
                Directory.CreateDirectory(Path.GetDirectoryName(FileSavePath));
                (Stream stream, long fileLength) = await Picacg.DownloadStream(Url);
                using (stream)
                using (FileStream fileStream = new(FileSavePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    TotalBytes = fileLength;
                    DownloadedBytes = 0;
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    var lastReport = DateTime.Now;
                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            Fail();
                            return;
                        }
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (await Task.WhenAny(readTask, Task.Delay(ReadBytesTimeout, token)) == readTask)
                        {
                            read = readTask.Result;
                            if (read == 0)
                            {
                                break;
                            }
                            await fileStream.WriteAsync(buffer.AsMemory(0, read), token);
                            totalRead += read;
                        }
                        else
                        {
                            throw new TimeoutException($"从流读取字节超时！");
                        }
                        if (fileLength > 0 && (DateTime.Now - lastReport).TotalMilliseconds > 100)
                        {
                            UpdateProgress(totalRead, fileLength);
                            lastReport = DateTime.Now;
                        }
                    }
                    // 循环外再回调一次，确保100%
                    UpdateProgress(totalRead, fileLength);
                    if (token.IsCancellationRequested)
                    {
                        Fail();
                        return;
                    }
                    if (totalRead == fileLength)
                    {
                        Complete();
                    }
                    else
                    {
                        Fail();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving stream to file: {ex.Message}");
                Fail();
            }
        }

        public void Complete()
        {
            OnCompleted?.Invoke(this);
        }

        public void Fail()
        {
            OnFailed?.Invoke(this);
        }

        public void UpdateProgress(long downloadedBytes, long totalBytes)
        {
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
            OnDownloadProgressUpdated?.Invoke(this, downloadedBytes, totalBytes, Percentage);
        }
    }
}
