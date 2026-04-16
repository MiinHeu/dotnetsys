using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class App : Application
{
	// Giữ reference tránh GC collect — kick-start auto-sync khi WiFi kết nối lại
	private readonly ConnectivityService _connectivity;

	public App(ConnectivityService connectivity)
	{
		InitializeComponent();
		_connectivity = connectivity;

		var langCode = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "en");
		var culture = new System.Globalization.CultureInfo(langCode);
		System.Globalization.CultureInfo.CurrentCulture = culture;
		System.Globalization.CultureInfo.CurrentUICulture = culture;
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var hasSelected = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.HasSelectedLanguage, false);
		if (hasSelected)
		{
			return new Window(new AppShell());
		}
		else
		{
			return new Window(new LanguageSelectionPage());
		}
	}
}