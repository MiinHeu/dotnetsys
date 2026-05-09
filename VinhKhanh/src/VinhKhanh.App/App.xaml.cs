using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class App : Application
{
	// Giữ reference tránh GC collect — kick-start auto-sync khi WiFi kết nối lại
	private readonly ConnectivityService _connectivity;
	private readonly RealtimeService _realtime;
	private readonly SessionTrackingService _sessionTracking;

	public App(ConnectivityService connectivity, RealtimeService realtime, SessionTrackingService sessionTracking)
	{
		InitializeComponent();
		_connectivity = connectivity;
		_realtime = realtime;
		_sessionTracking = sessionTracking;

		// TC-06: Chạy khởi động trong luồng phụ để tránh crash app nếu Server không phản hồi
		Task.Run(async () => {
			try {
				await _realtime.StartAsync();
				await _sessionTracking.StartSessionAsync();
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine($"[App] Start error: {ex.Message}");
			}
		});

		// Smart Language Detection: Lấy ngôn ngữ máy hoặc mặc định là "en"
		string langCode;
		if (!Microsoft.Maui.Storage.Preferences.ContainsKey(AppPreferences.UiLanguage))
		{
			// Lần đầu mở App: Tự động nhận diện
			// Thử lấy từ UICulture trước, nếu không được thì lấy Culture
			var detectedCulture = System.Globalization.CultureInfo.CurrentUICulture ?? System.Globalization.CultureInfo.CurrentCulture;
			var deviceLang = detectedCulture.TwoLetterISOLanguageName.ToLower();
			
			var supportedLangs = new List<string> { "vi", "en", "ko", "zh" };
			langCode = supportedLangs.Contains(deviceLang) ? deviceLang : "en";
			
			System.Diagnostics.Debug.WriteLine($"[App] Detected Device Lang: {deviceLang} -> Final: {langCode}");
			
			// Lưu lại làm lựa chọn tạm thời
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, langCode);
		}
		else
		{
			// Đã có lựa chọn trước đó
			langCode = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "en");
		}

		var culture = new System.Globalization.CultureInfo(langCode);
		System.Globalization.CultureInfo.CurrentCulture = culture;
		System.Globalization.CultureInfo.CurrentUICulture = culture;
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var hasSeenOnboarding = Microsoft.Maui.Storage.Preferences.Get("HasSeenOnboarding", false);
		var hasSelectedLang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.HasSelectedLanguage, false);

		if (!hasSeenOnboarding)
		{
			return new Window(new Pages.OnboardingPage());
		}
		else if (!hasSelectedLang)
		{
			return new Window(new LanguageSelectionPage());
		}
		else
		{
			return new Window(new AppShell());
		}
	}

	/// <summary>Khi App chuyển sang background — gửi session end.</summary>
	protected override void OnSleep()
	{
		base.OnSleep();
		_ = _sessionTracking.EndSessionAsync();
	}

	/// <summary>Khi App quay lại foreground — bắt đầu phiên mới.</summary>
	protected override void OnResume()
	{
		base.OnResume();
		_ = _sessionTracking.StartSessionAsync();
	}
}