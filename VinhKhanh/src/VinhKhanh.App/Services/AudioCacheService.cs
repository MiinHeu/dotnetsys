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
    private readonly string _cacheDir = Path.Combine(FileSystem.CacheDirectory, "AudioCache");

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
    public async Task<string?> GetAudioPathAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var fileName = GetHash(url) + ".mp3";
        var localPath = Path.Combine(_cacheDir, fileName);

        // 1. Kiểm tra xem tệp đã tồn tại trong máy chưa
        if (File.Exists(localPath))
        {
            Debug.WriteLine($"[AudioCache] Cache hit: {localPath}");
            return localPath;
        }

        // 2. Nếu chưa có, thực hiện tải về
        try
        {
            Debug.WriteLine($"[AudioCache] Cache miss, downloading: {url}");
            var bytes = await _httpClient.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(localPath, bytes, ct);
            Debug.WriteLine($"[AudioCache] Saved to: {localPath}");
            return localPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioCache] Download failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Thực hiện tải trước (Pre-fetch) tệp âm thanh một cách ngầm định.
    /// </summary>
    public async Task PreFetchAsync(string url)
    {
        try
        {
            _ = await GetAudioPathAsync(url);
        }
        catch { /* Bỏ qua lỗi khi tải ngầm */ }
    }

    private static string GetHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
