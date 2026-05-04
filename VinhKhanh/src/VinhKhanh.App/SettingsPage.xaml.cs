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
		MockGpsSwitch.IsToggled = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.MockGpsEnabled, false);
	}

	private async void OnSave(object? sender, EventArgs e)
	{
		var url = ApiUrlEntry.Text?.Trim();
		if (!string.IsNullOrEmpty(url))
		{
			if (!url.StartsWith("http"))
			{
				await DisplayAlert("Lỗi", "Địa chỉ API phải bắt đầu bằng http:// hoặc https://", "OK");
				return;
			}
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.ApiBaseUrl, url);
		}

		var m = RadiusMultEntry.Text?.Trim();
		if (!string.IsNullOrEmpty(m))
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.GpsRadiusMultiplier, m);

		Microsoft.Maui.Storage.Preferences.Set(AppPreferences.MockGpsEnabled, MockGpsSwitch.IsToggled);

		await DisplayAlert("Thành công", "Cài đặt đã được lưu. Vui lòng khởi động lại GPS nếu đang bật.", "Tuyệt vời");
	}
}
