using System.Collections.Specialized;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class MainPage : ContentPage
{
	private const double DefaultLatitude = 10.7535;
	private const double DefaultLongitude = 106.6783;
	private readonly MainViewModel _vm;
	private readonly NarrationService _narration;
	private PoiSnapshot? _selectedPoi;
	private bool _centerOnNextLocation = true;

	public MainPage()
	{
		InitializeComponent();
		_vm = MauiProgram.Services.GetRequiredService<MainViewModel>();
		_narration = MauiProgram.Services.GetRequiredService<NarrationService>();
		BindingContext = _vm;

		LangPicker.ItemsSource = new[] { "vi", "en", "zh", "ko", "ja" };
		LangPicker.SelectedItem = _vm.SelectedLanguage;

		InitializeMap();

		_vm.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MainViewModel.StatusMessage))
				Dispatcher.Dispatch(() => StatusLabel.Text = _vm.StatusMessage);
			if (e.PropertyName == nameof(MainViewModel.NearestLabel))
				Dispatcher.Dispatch(() => NearestLabel.Text = string.IsNullOrWhiteSpace(_vm.NearestLabel)
					? "Đang tìm điểm gần nhất..."
					: $"Gần nhất: {_vm.NearestLabel}");
			if (e.PropertyName == nameof(MainViewModel.NearestPoiId))
				Dispatcher.Dispatch(UpdatePins);
			if (e.PropertyName is nameof(MainViewModel.UserLatitude) or nameof(MainViewModel.UserLongitude))
				Dispatcher.Dispatch(UpdateMapUser);
			if (e.PropertyName == nameof(MainViewModel.IsTracking))
				Dispatcher.Dispatch(() =>
					TrackBtn.Text = _vm.IsTracking ? "Tắt GPS" : "Bật GPS");
		};

		_vm.Pois.CollectionChanged += OnPoisChanged;
		StatusLabel.Text = _vm.StatusMessage;
		NearestLabel.Text = "Đang tìm điểm gần nhất...";

		Loaded += async (_, _) =>
		{
			try
			{
				CenterMap(DefaultLatitude, DefaultLongitude);
				await _vm.SyncPoisCommand.ExecuteAsync(null);
				UpdatePins();
			}
			catch (Exception ex)
			{
				StatusLabel.Text = "Lỗi tải bản đồ.";
				System.Diagnostics.Debug.WriteLine($"MainPage Loaded error: {ex}");
			}
		};
	}

	private void InitializeMap()
	{
		if (StreetMap.Map.Layers.All(x => x.Name != "osm"))
		{
			var osm = OpenStreetMap.CreateTileLayer();
			osm.Name = "osm";
			StreetMap.Map.Layers.Add(osm);
		}

		StreetMap.Map.Widgets.Clear();
		StreetMap.UniqueCallout = true;
		StreetMap.MyLocationEnabled = true;
		StreetMap.MyLocationFollow = false;
	}

	private void OnPoisChanged(object? sender, NotifyCollectionChangedEventArgs e)
		=> Dispatcher.Dispatch(UpdatePins);

	private void OnLangChanged(object? sender, EventArgs e)
	{
		if (LangPicker.SelectedItem is string lang)
		{
			_vm.SelectedLanguage = lang;
			UpdatePins();
		}
	}

	private async void OnSyncClicked(object? sender, EventArgs e)
		=> await _vm.SyncPoisCommand.ExecuteAsync(null);

	private async void OnTrackClicked(object? sender, EventArgs e)
	{
		if (!_vm.IsTracking)
			_centerOnNextLocation = true;

		await _vm.ToggleTrackingCommand.ExecuteAsync(null);
	}

	private void UpdateMapUser()
	{
		if (!double.IsFinite(_vm.UserLatitude) || !double.IsFinite(_vm.UserLongitude))
			return;

		StreetMap.MyLocationLayer?.UpdateMyLocation(new Position(_vm.UserLatitude, _vm.UserLongitude), animated: true);

		if (_centerOnNextLocation)
		{
			CenterMap(_vm.UserLatitude, _vm.UserLongitude);
			_centerOnNextLocation = false;
		}

		StreetMap.RefreshGraphics();
	}

	private void CenterMap(double latitude, double longitude)
	{
		var sm = SphericalMercator.FromLonLat(longitude, latitude);
		var currentResolution = StreetMap.Map.Navigator.Viewport.Resolution;
		StreetMap.Map.Navigator.CenterOnAndZoomTo(
			new MPoint(sm.x, sm.y),
			currentResolution > 0 ? currentResolution : 2);
	}

	private void UpdatePins()
	{
		StreetMap.Pins.Clear();

		foreach (var p in _vm.Pois)
		{
			var isNearest = _vm.NearestPoiId == p.Id && _vm.NearestPoiId > 0;
			var pin = new Pin
			{
				Label = p.ResolveName(_vm.SelectedLanguage),
				Address = p.ResolveDescription(_vm.SelectedLanguage),
				Position = new Position(p.Latitude, p.Longitude),
				Type = PinType.Pin,
				Color = isNearest
					? Microsoft.Maui.Graphics.Color.FromArgb("#FF5A1F")
					: Microsoft.Maui.Graphics.Color.FromArgb("#111827"),
				Scale = isNearest ? 1.15f : 0.85f,
				Tag = p
			};
			StreetMap.Pins.Add(pin);

			if (isNearest)
				StreetMap.SelectedPin = pin;
		}

		StreetMap.RefreshGraphics();

		if (_selectedPoi != null)
		{
			var latest = _vm.Pois.FirstOrDefault(x => x.Id == _selectedPoi.Id);
			if (latest != null)
				ShowPoiDetail(latest);
			return;
		}

		if (_vm.NearestPoiId > 0)
		{
			var nearest = _vm.Pois.FirstOrDefault(x => x.Id == _vm.NearestPoiId);
			if (nearest != null)
				ShowPoiDetail(nearest);
		}
	}

	private void ShowPoiDetail(PoiSnapshot poi)
	{
		_selectedPoi = poi;
		PoiTitleLabel.Text = poi.ResolveName(_vm.SelectedLanguage);
		PoiDescLabel.Text = poi.ResolveDescription(_vm.SelectedLanguage);
		PoiDetailCard.IsVisible = true;
	}

	private void OnPinClicked(object? sender, PinClickedEventArgs e)
	{
		e.Handled = true;
		if (e.Pin?.Tag is PoiSnapshot poi)
			ShowPoiDetail(poi);
	}

	private void OnMapClicked(object? sender, MapClickedEventArgs e)
	{
		if (e.NumOfTaps < 1)
			return;

		PoiDetailCard.IsVisible = false;
		_selectedPoi = null;
	}

	private async void OnReadAudioClicked(object? sender, EventArgs e)
	{
		var poi = _selectedPoi ?? _vm.Pois.FirstOrDefault(x => x.Id == _vm.NearestPoiId);
		if (poi == null)
		{
			StatusLabel.Text = "Chưa có POI để phát.";
			System.Diagnostics.Debug.WriteLine("[MainPage] No POI selected for playback");
			return;
		}

		try
		{
			StatusLabel.Text = $"Đang phát: {poi.ResolveName(_vm.SelectedLanguage)}";
			var heardSeconds = await _narration.PlayPoiAsync(poi, _vm.SelectedLanguage, _vm.ApiRootForAudio);
			StatusLabel.Text = $"Đã phát xong ({heardSeconds}s): {poi.ResolveName(_vm.SelectedLanguage)}";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"Lỗi phát audio: {ex.Message}";
			System.Diagnostics.Debug.WriteLine($"[MainPage] Playback error: {ex}");
		}
	}
}
