using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _vm;
	private int _selectedPoiId;

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
			if (e.PropertyName == nameof(MainViewModel.NearestPoiId))
				Dispatcher.Dispatch(UpdatePins);
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
			try
			{
				UpdateMapUser();
				await _vm.SyncPoisCommand.ExecuteAsync(null);
				UpdatePins();
			}
			catch (Exception ex)
			{
				StatusLabel.Text = "Lỗi khởi tạo bản đồ. Kiểm tra Google Maps API Key.";
				System.Diagnostics.Debug.WriteLine($"MainPage Loaded error: {ex}");
			}
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
			var isNearest = _vm.NearestPoiId == p.Id && _vm.NearestPoiId > 0;
			var pin = new Pin
			{
				Label = isNearest ? $"★ {p.ResolveName(_vm.SelectedLanguage)}" : p.ResolveName(_vm.SelectedLanguage),
				Address = p.ResolveDescription(_vm.SelectedLanguage),
				Location = new Location(p.Latitude, p.Longitude),
				Type = PinType.Place
			};
			pin.MarkerClicked += (_, __) =>
			{
				_selectedPoiId = p.Id;
				PoiTitleLabel.Text = p.ResolveName(_vm.SelectedLanguage);
				PoiDescLabel.Text = p.ResolveDescription(_vm.SelectedLanguage);
				PoiDetailCard.IsVisible = true;
			};
			StreetMap.Pins.Add(pin);
		}

		if (_selectedPoiId == 0 && _vm.NearestPoiId > 0)
		{
			var nearest = _vm.Pois.FirstOrDefault(x => x.Id == _vm.NearestPoiId);
			if (nearest != null)
			{
				PoiTitleLabel.Text = nearest.ResolveName(_vm.SelectedLanguage);
				PoiDescLabel.Text = nearest.ResolveDescription(_vm.SelectedLanguage);
				PoiDetailCard.IsVisible = true;
			}
		}
	}
}
