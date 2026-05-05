using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices;
using VinhKhanh.App.Models;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.Services;

public sealed class ApiClientService
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter() }
	};

	private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };
	
	public HttpClient CreateClient()
	{
		return _http;
	}

	private string GetBaseUrl()
	{
		var defaultUrl = GetDefaultApiBase();
		var baseUrl = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, defaultUrl);

		// Ép dùng Azure trên máy thật nếu URL mặc định là Azure
		if (DeviceInfo.DeviceType != DeviceType.Virtual && defaultUrl.Contains("azurewebsites.net"))
		{
			baseUrl = defaultUrl;
		}

		if (!baseUrl.EndsWith('/')) baseUrl += "/";
		return baseUrl;
	}

	public static string GetDefaultApiBase()
	{
		// 1. Hỗ trợ giả lập (Emulator) để lập trình viên test cục bộ
		if (DeviceInfo.DeviceType == DeviceType.Virtual)
		{
#if ANDROID
			return "http://10.0.2.2:5283/";
#else
			return "http://localhost:5283/";
#endif
		}

		// 2. Mặc định ưu tiên Azure cho máy thật/phiên bản phát hành (theo NewBranch)
		return "https://vinh-khanh-food-street-gvhceeg4gbakhjgc.eastasia-01.azurewebsites.net/";
	}

	public async Task<IReadOnlyList<PoiSnapshot>> GetPoisAsync(string lang, CancellationToken ct = default)
	{
		var http = CreateClient();
		var url = $"{GetBaseUrl()}api/poi?lang={Uri.EscapeDataString(lang)}&t={DateTime.UtcNow.Ticks}";
		var list = await http.GetFromJsonAsync<List<PoiSnapshot>>(url, JsonOpts, ct);
		return list ?? [];
	}

	public async Task<IReadOnlyList<TourSnapshot>> GetToursAsync(string lang, CancellationToken ct = default)
	{
		var http = CreateClient();
		var url = $"{GetBaseUrl()}api/tour?lang={Uri.EscapeDataString(lang)}&t={DateTime.UtcNow.Ticks}";
		var list = await http.GetFromJsonAsync<List<TourSnapshot>>(url, JsonOpts, ct);
		return list ?? [];
	}

	public async Task<PoiSnapshot?> GetPoiAsync(int id, CancellationToken ct = default)
	{
		var http = CreateClient();
		var url = $"{GetBaseUrl()}api/poi/{id}?t={DateTime.UtcNow.Ticks}";
		return await http.GetFromJsonAsync<PoiSnapshot>(url, JsonOpts, ct);
	}

	public async Task<PoiSnapshot?> GetPoiByQrCodeAsync(string qrCode, CancellationToken ct = default)
	{
		var code = Uri.EscapeDataString(qrCode.Trim());
		var http = CreateClient();
		var url = $"{GetBaseUrl()}api/poi/qrcode/{code}?t={DateTime.UtcNow.Ticks}";
		var res = await http.GetAsync(url, ct);
		if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
		res.EnsureSuccessStatusCode();
		return await res.Content.ReadFromJsonAsync<PoiSnapshot>(JsonOpts, ct);
	}

	public async Task<bool> TryPostMovementBatchAsync(MovementBatchDto dto, CancellationToken ct = default)
	{
		try
		{
			var http = CreateClient();
			var url = $"{GetBaseUrl()}api/movement/batch";
			var res = await http.PostAsJsonAsync(url, dto, ct);
			return res.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	public async Task<bool> TryPostHistoryLogAsync(AppHistoryLogDto dto, CancellationToken ct = default)
	{
		try
		{
			var http = CreateClient();
			var url = $"{GetBaseUrl()}api/history/log";
			var res = await http.PostAsJsonAsync(url, dto, ct);
			return res.IsSuccessStatusCode;
		}
		catch { return false; }
	}

	public async Task<bool> TryPostAnalyticsVisitAsync(VisitLogDto dto, CancellationToken ct = default)
	{
		try
		{
			var http = CreateClient();
			var url = $"{GetBaseUrl()}api/analytics/log";
			var res = await http.PostAsJsonAsync(url, dto, ct);
			return res.IsSuccessStatusCode;
		}
		catch { return false; }
	}

	public async Task PostMovementBatchAsync(MovementBatchDto dto, CancellationToken ct = default)
	{
		_ = await TryPostMovementBatchAsync(dto, ct);
	}

	public async Task PostHistoryLogAsync(AppHistoryLogDto dto, CancellationToken ct = default)
	{
		_ = await TryPostHistoryLogAsync(dto, ct);
	}

	public async Task PostAnalyticsVisitAsync(VisitLogDto dto, CancellationToken ct = default)
	{
		_ = await TryPostAnalyticsVisitAsync(dto, ct);
	}

	public string ApiRoot => Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, GetDefaultApiBase()).TrimEnd('/');

	public async Task<string?> ChatAsync(ChatRequest req, CancellationToken ct = default)
	{
		var http = CreateClient();
		var url = $"{GetBaseUrl()}api/ai/chat";
		var res = await http.PostAsJsonAsync(url, req, ct);
		res.EnsureSuccessStatusCode();
		var json = await res.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		return doc.RootElement.TryGetProperty("reply", out var r) ? r.GetString() : null;
	}
}
