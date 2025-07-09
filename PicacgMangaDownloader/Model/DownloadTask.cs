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

        public int RetryCount { get; set; } = 0;

        public int MaxRetry { get; set; } = 3;

        public DownloadStatus DownloadStatus { get; set; } = DownloadStatus.NotDownloaded;

        public async Task StartAsync(CancellationToken token, bool force = false)
        {
            if (DownloadStatus == DownloadStatus.Downloaded && !force)
            {
                return;
            }
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
            DownloadStatus = DownloadStatus.Downloading;
            RetryCount = 0;
            while (RetryCount < MaxRetry && !token.IsCancellationRequested)
            {
                try
                {
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
                            return;
                        }
                        else
                        {
                            throw new IOException($"下载的文件大小与预期不符: {totalRead} != {fileLength}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    RetryCount++;
                    if (RetryCount < MaxRetry && !token.IsCancellationRequested)
                    {
                        await Task.Delay(3000, token);
                    }
                }
            }
            Fail();
        }

        public void Complete()
        {
            OnCompleted?.Invoke(this);
            DownloadStatus = DownloadStatus.Downloaded;
        }

        public void Fail()
        {
            OnFailed?.Invoke(this);
            DownloadStatus = DownloadStatus.DownloadFailed;
        }

        public void UpdateProgress(long downloadedBytes, long totalBytes)
        {
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
            OnDownloadProgressUpdated?.Invoke(this, downloadedBytes, totalBytes, Percentage);
        }
    }
}