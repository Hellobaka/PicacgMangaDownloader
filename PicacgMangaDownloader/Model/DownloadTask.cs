using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicacgMangaDownloader.Model
{
    public class DownloadTask
    {
        public static event Action<DownloadTask> OnCompleted;
        public static event Action<DownloadTask> OnFailed;
        public static event Action<DownloadTask, long, long, double> OnDownloadProgressUpdated;

        public string? Url { get; set; }

        public string? FileSavePath { get; set; }

        public TimeSpan ReadBytesTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public long TotalBytes { get; set; }

        public long DownloadedBytes { get; set; }

        public double Percentage => TotalBytes == 0 ? 0 : (double)DownloadedBytes / TotalBytes * 100;

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
