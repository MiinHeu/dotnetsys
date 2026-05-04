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

		_ = _realtime.StartAsync();
		_ = _sessionTracking.StartSessionAsync();

		var langCode = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "en");
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