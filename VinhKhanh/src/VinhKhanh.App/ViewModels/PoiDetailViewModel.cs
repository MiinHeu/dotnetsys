using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;
using VinhKhanh.App.Models;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.ViewModels;

[QueryProperty(nameof(PoiId), "PoiId")]
[QueryProperty(nameof(AutoPlay), "AutoPlay")]
[QueryProperty(nameof(TriggerType), "TriggerType")]
public partial class PoiDetailViewModel(
	NarrationService narration, 
	ApiClientService api,
	SessionService session,
	IOutboxService outbox) : ObservableObject
{
	private PoiSnapshot? _poiDetail;
	public PoiSnapshot? PoiDetail
	{
		get => _poiDetail;
		set
		{
			if (SetProperty(ref _poiDetail, value))
			{
				OnPoiDetailChanged(value);
				OnPropertyChanged(nameof(DisplayName));
				OnPropertyChanged(nameof(DisplayDescription));
				OnPropertyChanged(nameof(Images));
				OnPropertyChanged(nameof(MenuItems));
			}
		}
	}

	public List<string> Images {
		get {
			try {
				return string.IsNullOrWhiteSpace(PoiDetail?.ImagesJson) 
					? new List<string>() 
					: System.Text.Json.JsonSerializer.Deserialize<List<string>>(PoiDetail.ImagesJson) ?? new List<string>();
			} catch { return new List<string>(); }
		}
	}

	public List<string> MenuItems {
		get {
			try {
				return string.IsNullOrWhiteSpace(PoiDetail?.MenuJson) 
					? new List<string>() 
					: System.Text.Json.JsonSerializer.Deserialize<List<string>>(PoiDetail.MenuJson) ?? new List<string>();
			} catch { return new List<string>(); }
		}
	}

	[ObservableProperty] private int _poiId;
	[ObservableProperty] private bool _autoPlay;
	[ObservableProperty] private string? _triggerType;

	[ObservableProperty] private bool _isPlaying;

	partial void OnPoiIdChanged(int value)
	{
		if (value <= 0) return;
		
		// TC-07: Chạy ngầm và bọc try-catch để tránh crash app nếu API lỗi
		Task.Run(async () =>
		{
			try
			{
				var data = await api.GetPoiAsync(value);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					PoiDetail = data;
					if (PoiDetail != null)
					{
						_ = narration.PreFetchAsync(PoiDetail, SelectedLanguage);
					}
				});
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[PoiDetail] Load error: {ex.Message}");
			}
		});
	}

	[ObservableProperty] 
	[NotifyPropertyChangedFor(nameof(DisplayName))]
	[NotifyPropertyChangedFor(nameof(DisplayDescription))]
	[NotifyPropertyChangedFor(nameof(PageTitle))]
	[NotifyPropertyChangedFor(nameof(PlayNarrationText))]
	[NotifyPropertyChangedFor(nameof(StopNarrationText))]
	[NotifyPropertyChangedFor(nameof(PlaybackLabel))]
	private string _selectedLanguage = "vi";

	partial void OnSelectedLanguageChanged(string value)
	{
		if (PoiDetail != null)
		{
			_ = narration.PreFetchAsync(PoiDetail, value);
		}
	}

	public string DisplayName => PoiDetail?.ResolveName(SelectedLanguage) ?? "";
	public string DisplayDescription => PoiDetail?.ResolveDescription(SelectedLanguage) ?? "";
	
	public string PageTitle => VinhKhanh.App.Resources.Strings.AppResources.TabPois; // Dùng chung key "Quán ăn" hoặc "Restaurants"
	public string PlayNarrationText => VinhKhanh.App.Resources.Strings.AppResources.PlayNarration;
	public string StopNarrationText => VinhKhanh.App.Resources.Strings.AppResources.StopNarration;
	public string PlaybackLabel => VinhKhanh.App.Resources.Strings.AppResources.PlaybackPoiLabel;

	private void OnPoiDetailChanged(PoiSnapshot? value)
	{
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");

		if (AutoPlay && value != null)
		{
			// Thực hiện phát thuyết minh sau một khoảng trễ ngắn để đảm bảo giao diện đã load xong
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await Task.Delay(500); // Đợi 0.5s để hiệu ứng chuyển trang mượt mà
				await HandleAutoPlayAsync(value);
			});
		}
	}

	private async Task HandleAutoPlayAsync(PoiSnapshot poi)
	{
		if (IsPlaying) return;

		var lang = SelectedLanguage;
		var apiRoot = api.ApiRoot;

		// 1. Phát âm thanh
		IsPlaying = true;
		var heard = await narration.PlayPoiAsync(poi, lang, apiRoot);
		IsPlaying = false;

		// 2. Ghi nhật ký truy cập (Visit Log)
		var trigger = TriggerType ?? "MANUAL";
		var visit = new VisitLogDto(poi.Id, session.SessionId, lang, trigger, heard);
		if (!await api.TryPostAnalyticsVisitAsync(visit))
			await outbox.EnqueueVisitAsync(visit);

		// 3. Ghi lịch sử ứng dụng
		var history = new AppHistoryLogDto(session.SessionId, "QR_AUTO_VIEW", 
			PoiId: poi.Id, LanguageCode: lang);
		if (!await api.TryPostHistoryLogAsync(history))
			await outbox.EnqueueHistoryAsync(history);
	}


	[RelayCommand]
	private async Task PlayNarrationAsync()
	{
		if (PoiDetail == null) return;
		await HandleAutoPlayAsync(PoiDetail);
	}

	[RelayCommand]
	private async Task StopNarrationAsync()
	{
		await narration.StopAsync();
		IsPlaying = false;
	}

	[RelayCommand]
	private static Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
