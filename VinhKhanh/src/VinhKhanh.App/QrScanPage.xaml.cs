using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.Shared.DTOs;
using ZXing.Net.Maui;

namespace VinhKhanh.App;

public partial class QrScanPage : ContentPage
{
	private readonly ApiClientService _api;
	private readonly NarrationService _narration;
	private readonly SessionService _session;
	private readonly IOutboxService _outbox;

	// SemaphoreSlim đảm bảo chỉ 1 luồng xử lý tại một thời điểm — tránh race condition
	private readonly SemaphoreSlim _processingLock = new(1, 1);
	private DateTime _lastHandled = DateTime.MinValue;

	public QrScanPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClientService>();
		_narration = MauiProgram.Services.GetRequiredService<NarrationService>();
		_session = MauiProgram.Services.GetRequiredService<SessionService>();
		_outbox = MauiProgram.Services.GetRequiredService<IOutboxService>();

		Scanner.Options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional | BarcodeFormats.OneDimensional,
			AutoRotate = true,
			Multiple = false,
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await PrepareCameraAsync();
	}

	protected override void OnDisappearing()
	{
		// Tắt scanner khi rời tab để tiết kiệm pin và tránh callback rác
		Scanner.IsDetecting = false;
		Scanner.IsVisible = false;
		base.OnDisappearing();
	}

	private async Task PrepareCameraAsync()
	{
		try
		{
			var permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (permission != PermissionStatus.Granted)
				permission = await Permissions.RequestAsync<Permissions.Camera>();

			if (permission != PermissionStatus.Granted)
			{
				Scanner.IsVisible = false;
				Scanner.IsDetecting = false;
				StatusLabel.Text = "Khong co quyen camera. Ban co the nhap ma QR thu cong o ben duoi.";
				return;
			}

			var cameras = await Scanner.GetAvailableCameras();
			if (cameras == null || cameras.Count == 0)
			{
				Scanner.IsVisible = false;
				Scanner.IsDetecting = false;
				StatusLabel.Text = "Khong tim thay camera. Ban co the nhap ma QR thu cong o ben duoi.";
				return;
			}

			Scanner.SelectedCamera = cameras[0];
			Scanner.IsVisible = true;
			Scanner.IsDetecting = true;
			StatusLabel.Text = "San sang quet QR...";
		}
		catch (Exception ex)
		{
			Scanner.IsVisible = false;
			Scanner.IsDetecting = false;
			StatusLabel.Text = $"Khong khoi tao duoc camera: {ex.Message}";
			System.Diagnostics.Debug.WriteLine($"[QrScanPage] PrepareCameraAsync error: {ex}");
		}
	}

	// ZXing gọi callback này từ camera background thread — PHẢI dispatch về main thread
	private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		var text = e.Results?.FirstOrDefault()?.Value;
		if (string.IsNullOrWhiteSpace(text)) return;

		// Kiểm tra cooldown sớm trước khi dispatch để tránh queue nhiều lần
		if ((DateTime.UtcNow - _lastHandled).TotalSeconds < 3) return;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await HandleQrValueAsync(text);
		});
	}

	private async void OnManualSubmit(object? sender, EventArgs e)
	{
		await HandleQrValueAsync(ManualCodeEntry.Text);
	}

	private async Task HandleQrValueAsync(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw)) return;

		// Semaphore đảm bảo chỉ 1 luồng xử lý — không bị double-trigger
		if (!_processingLock.Wait(0)) return;

		try
		{
			// Double-check cooldown sau khi acquire lock
			if ((DateTime.UtcNow - _lastHandled).TotalSeconds < 3) return;
			_lastHandled = DateTime.UtcNow;

			StatusLabel.Text = "Dang xu ly ma QR...";

			var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
			var apiRoot = Microsoft.Maui.Storage.Preferences.Get(
				AppPreferences.ApiBaseUrl,
				ApiClientService.GetDefaultApiBase()).TrimEnd('/');

			// Dừng scanner ngay để tránh quét lại trong khi đang xử lý
			Scanner.IsDetecting = false;

			PoiSnapshot? poi = await _api.GetPoiByQrCodeAsync(raw.Trim());

			if (poi == null)
			{
				var id = TryParsePoiId(raw);
				if (id == null)
				{
					StatusLabel.Text = "Khong nhan dang duoc ma QR. Vui long thu lai.";
					return;
				}

				StatusLabel.Text = "Dang tai thong tin...";
				poi = await _api.GetPoiAsync(id.Value);
			}

			if (poi == null)
			{
				StatusLabel.Text = "Khong tim thay diem tham quan nay.";
				return;
			}

			StatusLabel.Text = $"Dang phat: {poi.ResolveName(lang)}";

			var heard = await _narration.PlayPoiAsync(poi, lang, apiRoot);

			// Analytics — fire and forget với outbox fallback
			var visit = new VisitLogDto(poi.Id, _session.SessionId, lang, "QR", heard);
			if (!await _api.TryPostAnalyticsVisitAsync(visit))
				await _outbox.EnqueueVisitAsync(visit);

			var history = new AppHistoryLogDto(_session.SessionId, "QR_SCAN",
				PoiId: poi.Id, LanguageCode: lang);
			if (!await _api.TryPostHistoryLogAsync(history))
				await _outbox.EnqueueHistoryAsync(history);

			StatusLabel.Text = $"Da phat xong: {poi.ResolveName(lang)}";
			ManualCodeEntry.Text = string.Empty;
		}
		catch (Exception ex)
		{
			StatusLabel.Text = "Co loi xay ra. Vui long thu lai.";
			System.Diagnostics.Debug.WriteLine($"[QrScanPage] HandleQrValueAsync error: {ex}");
			// Reset cooldown để user có thể thử lại ngay
			_lastHandled = DateTime.MinValue;
		}
		finally
		{
			// Bật lại scanner sau khi xử lý xong
			if (Scanner.IsVisible)
				Scanner.IsDetecting = true;

			_processingLock.Release();
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
