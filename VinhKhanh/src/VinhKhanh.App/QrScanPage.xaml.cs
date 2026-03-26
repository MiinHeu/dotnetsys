using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Services;
using VinhKhanh.Shared.DTOs;
using ZXing.Net.Maui;

namespace VinhKhanh.App;

public partial class QrScanPage : ContentPage
{
	private readonly ApiClientService _api;
	private readonly NarrationService _narration;
	private readonly SessionService _session;
	private DateTime _lastHandled = DateTime.MinValue;

	public QrScanPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClientService>();
		_narration = MauiProgram.Services.GetRequiredService<NarrationService>();
		_session = MauiProgram.Services.GetRequiredService<SessionService>();
		Scanner.Options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional | BarcodeFormats.OneDimensional,
			AutoRotate = true
		};
	}

	private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		if ((DateTime.UtcNow - _lastHandled).TotalSeconds < 4)
			return;

		var text = e.Results?.FirstOrDefault()?.Value;
		if (string.IsNullOrWhiteSpace(text)) return;

		_lastHandled = DateTime.UtcNow;
		Dispatcher.Dispatch(() => StatusLabel.Text = "Đang xử lý mã QR...");

		try
		{
			var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
			var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');

			var key = text.Trim();
			var poi = await _api.GetPoiByQrCodeAsync(key);
			if (poi == null)
			{
				var id = TryParsePoiId(text);
				if (id == null)
				{
					Dispatcher.Dispatch(() => StatusLabel.Text = "Không nhận dạng được mã (dùng mã VK-POI-xxx hoặc ID).");
					return;
				}
				Dispatcher.Dispatch(() => StatusLabel.Text = $"Đang tải POI #{id}...");
				poi = await _api.GetPoiAsync(id.Value);
			}

			if (poi == null)
			{
				Dispatcher.Dispatch(() => StatusLabel.Text = "Không tìm thấy POI.");
				return;
			}

			var heard = await _narration.PlayPoiAsync(poi, lang, apiRoot);
			await _api.PostAnalyticsVisitAsync(new VisitLogDto(poi.Id, _session.SessionId, lang, "QR", heard));
			await _api.PostHistoryLogAsync(new AppHistoryLogDto(_session.SessionId, "QR_SCAN",
				PoiId: poi.Id, LanguageCode: lang));

			Dispatcher.Dispatch(() => StatusLabel.Text = $"Đã phát: {poi.ResolveName(lang)}");
		}
		catch (Exception ex)
		{
			Dispatcher.Dispatch(() => StatusLabel.Text = $"Lỗi: {ex.Message}");
		}
	}

	private static int? TryParsePoiId(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw)) return null;
		raw = raw.Trim();
		var m = Regex.Match(raw, @"vk-poi-(\d+)", RegexOptions.IgnoreCase);
		if (m.Success && int.TryParse(m.Groups[1].Value, out var a)) return a;
		m = Regex.Match(raw, @"/poi/(\d+)", RegexOptions.IgnoreCase);
		if (m.Success && int.TryParse(m.Groups[1].Value, out var b)) return b;
		if (int.TryParse(raw, out var c)) return c;
		return null;
	}
}
