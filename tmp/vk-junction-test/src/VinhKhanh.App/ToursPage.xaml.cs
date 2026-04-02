using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Services;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class ToursPage : ContentPage
{
	private readonly ToursViewModel _vm;

	public ToursPage()
	{
		InitializeComponent();
		_vm = MauiProgram.Services.GetRequiredService<ToursViewModel>();
		BindingContext = _vm;

		Loaded += async (_, _) =>
		{
			_vm.Lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
			await _vm.LoadCommand.ExecuteAsync(null);
		};
	}

	private async void OnReload(object? sender, EventArgs e)
		=> await _vm.LoadCommand.ExecuteAsync(null);
}
