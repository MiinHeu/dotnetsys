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
	private readonly IOutboxService _outbox;
	private bool _isProcessing = false;
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
			AutoRotate = true
		};
	}

	protected override async void OnAppearing()
	{
		_isProcessing = false; // Reset trạng thái khi quay lại trang
		base.OnAppearing();
		await PrepareCameraAsync();
	}

	protected override void OnDisappearing()
	{
		Scanner.IsDetecting = false;
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
		}
	}

	private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
	{
		var text = e.Results?.FirstOrDefault()?.Value;
		await HandleQrValueAsync(text);
	}

	private async void OnManualSubmit(object? sender, EventArgs e)
	{
		await HandleQrValueAsync(ManualCodeEntry.Text);
	}

	private async Task HandleQrValueAsync(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw) || _isProcessing)
			return;

		if ((DateTime.UtcNow - _lastHandled).TotalSeconds < 2)
			return;

		_isProcessing = true; // Bắt đầu xử lý
		_lastHandled = DateTime.UtcNow;

		// Ép Camera dừng đồng bộ và đợi cho đến khi hoàn tất
		await MainThread.InvokeOnMainThreadAsync(() => 
		{
			Scanner.IsDetecting = false; // Dừng quét ngay lập tức
			StatusLabel.Text = "Dang xu ly ma QR...";
		});

		// Bước đệm an toàn: Đợi một chút để driver camera Android ổn định luồng xử lý
		await Task.Delay(150);

		try
		{
			var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
			var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');

			var key = raw.Trim();
			var poi = await _api.GetPoiByQrCodeAsync(key);
			if (poi == null)
			{
				var id = TryParsePoiId(raw);
				if (id == null)
				{
					MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = "Khong nhan dang duoc ma (dung VK-POI-xxx hoac ID).");
					return;
				}

				MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = $"Dang tai POI #{id}...");
				poi = await _api.GetPoiAsync(id.Value);
			}

			if (poi == null)
			{
				MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = VinhKhanh.App.Resources.Strings.AppResources.QrNotFoundStatus);
				return;
			}

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				StatusLabel.Text = string.Format(VinhKhanh.App.Resources.Strings.AppResources.QrFoundSatus, poi.ResolveName(lang));
				ManualCodeEntry.Text = string.Empty;

				// Tự động chuyển sang Tab Quán ăn và mở trang Chi tiết
				// Chỉ truyền ID để tránh lỗi ép kiểu (IConvertible) trên Android
				var navigationParameter = new Dictionary<string, object>
				{
					{ "PoiId", poi.Id },
					{ "AutoPlay", true },
					{ "TriggerType", "QR" }
				};
				await Shell.Current.GoToAsync($"//PoiListPage/{nameof(PoiDetailPage)}", navigationParameter);
			});
		}
		catch (Exception ex)
		{
			MainThread.BeginInvokeOnMainThread(() => 
			{
				StatusLabel.Text = $"Loi: {ex.Message}. Kiem tra IP trong Settings.";
				_isProcessing = false; // Cho phép quét lại nếu lỗi
			});
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
