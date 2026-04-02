using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.App.Models;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.Services;

public sealed class ApiClientService
{
	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public HttpClient CreateClient()
	{
		var baseUrl = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, GetDefaultApiBase());
		if (!baseUrl.EndsWith('/')) baseUrl += "/";
		return new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(45) };
	}

	public static string GetDefaultApiBase()
	{
#if ANDROID
		return "http://10.0.2.2:5283/";
#elif IOS || MACCATALYST
		return "http://127.0.0.1:5283/";
#else
		return "http://localhost:5283/";
#endif
	}

	public async Task<IReadOnlyList<PoiSnapshot>> GetPoisAsync(string lang, CancellationToken ct = default)
	{
		using var http = CreateClient();
		var list = await http.GetFromJsonAsync<List<PoiSnapshot>>($"api/poi?lang={Uri.EscapeDataString(lang)}", JsonOpts, ct);
		return list ?? [];
	}

	public async Task<IReadOnlyList<TourSnapshot>> GetToursAsync(string lang, CancellationToken ct = default)
	{
		using var http = CreateClient();
		var list = await http.GetFromJsonAsync<List<TourSnapshot>>($"api/tour?lang={Uri.EscapeDataString(lang)}", JsonOpts, ct);
		return list ?? [];
	}

	public async Task<PoiSnapshot?> GetPoiAsync(int id, CancellationToken ct = default)
	{
		using var http = CreateClient();
		return await http.GetFromJsonAsync<PoiSnapshot>($"api/poi/{id}", JsonOpts, ct);
	}

	public async Task<PoiSnapshot?> GetPoiByQrCodeAsync(string qrCode, CancellationToken ct = default)
	{
		var code = Uri.EscapeDataString(qrCode.Trim());
		using var http = CreateClient();
		var res = await http.GetAsync($"api/poi/qrcode/{code}", ct);
		if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
		res.EnsureSuccessStatusCode();
		return await res.Content.ReadFromJsonAsync<PoiSnapshot>(JsonOpts, ct);
	}

	public async Task<bool> TryPostMovementBatchAsync(MovementBatchDto dto, CancellationToken ct = default)
	{
		try
		{
			using var http = CreateClient();
			var res = await http.PostAsJsonAsync("api/movement/batch", dto, ct);
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
			using var http = CreateClient();
			var res = await http.PostAsJsonAsync("api/history/log", dto, ct);
			return res.IsSuccessStatusCode;
		}
		catch { return false; }
	}

	public async Task<bool> TryPostAnalyticsVisitAsync(VisitLogDto dto, CancellationToken ct = default)
	{
		try
		{
			using var http = CreateClient();
			var res = await http.PostAsJsonAsync("api/analytics/log", dto, ct);
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
		using var http = CreateClient();
		var res = await http.PostAsJsonAsync("api/ai/chat", req, ct);
		res.EnsureSuccessStatusCode();
		var json = await res.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		return doc.RootElement.TryGetProperty("reply", out var r) ? r.GetString() : null;
	}
}
