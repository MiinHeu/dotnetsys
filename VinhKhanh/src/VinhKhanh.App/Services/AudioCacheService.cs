using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace VinhKhanh.App.Services;

/// <summary>
/// Dịch vụ quản lý bộ nhớ đệm (Cache) cho các tệp âm thanh thuyết minh.
/// Giúp phát âm thanh ngay lập tức nếu đã tải về máy trước đó.
/// </summary>
public sealed class AudioCacheService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly string _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "AudioCache");
    private readonly SemaphoreSlim _downloadSemaphore = new(3, 3); // Chỉ cho phép 3 lượt tải cùng lúc

    public AudioCacheService()
    {
        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    /// <summary>
    /// Lấy đường dẫn tệp âm thanh cục bộ. Nếu chưa có, sẽ tải về máy.
    /// </summary>
    public async Task<string?> GetAudioPathAsync(string url, bool highPriority = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var fileName = GetHash(url) + ".mp3";
        var localPath = Path.Combine(_cacheDir, fileName);

        // 1. Kiểm tra xem tệp đã tồn tại trong máy chưa
        if (File.Exists(localPath))
        {
            Debug.WriteLine($"[AudioCache] Hit: {localPath}");
            return localPath;
        }

        // 2. Nếu chưa có, thực hiện tải về (Dùng semaphore để tránh làm nghẽn mạng, trừ khi là ưu tiên cao)
        if (!highPriority) await _downloadSemaphore.WaitAsync(ct);
        try
        {
            // Kiểm tra lại lần nữa sau khi qua semaphore nhỡ đâu file vừa được tải xong bởi thread khác
            if (File.Exists(localPath)) return localPath;

            Debug.WriteLine($"[AudioCache] Downloading: {url}");
            var bytes = await _httpClient.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(localPath, bytes, ct);
            Debug.WriteLine($"[AudioCache] Saved to: {localPath}");
            return localPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioCache] Download failed: {url} -> {ex.Message}");
            return null;
        }
        finally
        {
            if (!highPriority) _downloadSemaphore.Release();
        }
    }

    public async Task PreFetchAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        
        var fileName = GetHash(url) + ".mp3";
        if (File.Exists(Path.Combine(_cacheDir, fileName))) return;

        try { _ = await GetAudioPathAsync(url); } catch { }
    }

    private static string GetHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
