using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _vm;

	public MainPage()
	{
		InitializeComponent();
		_vm = MauiProgram.Services.GetRequiredService<MainViewModel>();
		BindingContext = _vm;

		LangPicker.ItemsSource = new[] { "vi", "en", "zh", "ko", "ja" };
		LangPicker.SelectedItem = _vm.SelectedLanguage;

		_vm.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MainViewModel.StatusMessage))
				StatusLabel.Text = _vm.StatusMessage;
			if (e.PropertyName == nameof(MainViewModel.NearestLabel))
				NearestLabel.Text = string.IsNullOrEmpty(_vm.NearestLabel)
					? ""
					: $"Gần nhất: {_vm.NearestLabel}";
			if (e.PropertyName is nameof(MainViewModel.UserLatitude) or nameof(MainViewModel.UserLongitude))
				Dispatcher.Dispatch(UpdateMapUser);
			if (e.PropertyName == nameof(MainViewModel.IsTracking))
				TrackBtn.Text = _vm.IsTracking ? "Tắt GPS" : "Bật GPS";
		};

		_vm.Pois.CollectionChanged += OnPoisChanged;

		StatusLabel.Text = _vm.StatusMessage;
		NearestLabel.Text = "";

		Loaded += async (_, _) =>
		{
			UpdateMapUser();
			await _vm.SyncPoisCommand.ExecuteAsync(null);
			UpdatePins();
		};
	}

	private void OnPoisChanged(object? sender, NotifyCollectionChangedEventArgs e)
		=> Dispatcher.Dispatch(UpdatePins);

	private void OnLangChanged(object? sender, EventArgs e)
	{
		if (LangPicker.SelectedItem is string lang)
			_vm.SelectedLanguage = lang;
	}

	private async void OnSyncClicked(object? sender, EventArgs e)
		=> await _vm.SyncPoisCommand.ExecuteAsync(null);

	private async void OnTrackClicked(object? sender, EventArgs e)
		=> await _vm.ToggleTrackingCommand.ExecuteAsync(null);

	private void UpdateMapUser()
	{
		var pos = new Location(_vm.UserLatitude, _vm.UserLongitude);
		StreetMap.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(0.35)));
	}

	private void UpdatePins()
	{
		StreetMap.Pins.Clear();
		foreach (var p in _vm.Pois)
		{
			StreetMap.Pins.Add(new Pin
			{
				Label = p.ResolveName(_vm.SelectedLanguage),
				Address = p.ResolveDescription(_vm.SelectedLanguage),
				Location = new Location(p.Latitude, p.Longitude),
				Type = PinType.Place
			});
		}
	}
}
