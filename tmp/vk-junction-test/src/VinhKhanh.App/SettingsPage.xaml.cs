using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		ApiUrlEntry.Text = Microsoft.Maui.Storage.Preferences.Get(
			AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase());
		RadiusMultEntry.Text = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.GpsRadiusMultiplier, "1");
	}

	private async void OnSave(object? sender, EventArgs e)
	{
		var url = ApiUrlEntry.Text?.Trim();
		if (!string.IsNullOrEmpty(url))
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.ApiBaseUrl, url);

		var m = RadiusMultEntry.Text?.Trim();
		if (!string.IsNullOrEmpty(m))
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.GpsRadiusMultiplier, m);

		await DisplayAlertAsync("Đã lưu", "Khởi động lại theo dõi GPS nếu đang bật.", "OK");
	}
}
