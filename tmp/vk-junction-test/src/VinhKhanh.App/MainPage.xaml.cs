using System.Collections.Specialized;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class MainPage : ContentPage
{
	private readonly MainViewModel _vm;
	private readonly NarrationService _narration;
	private PoiSnapshot? _selectedPoi;

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
				StatusLabel.Text = _vm.StatusMessage;
			if (e.PropertyName == nameof(MainViewModel.NearestLabel))
				NearestLabel.Text = string.IsNullOrWhiteSpace(_vm.NearestLabel)
					? "Dang tim diem gan nhat..."
					: $"Gan ban: {_vm.NearestLabel}";
			if (e.PropertyName == nameof(MainViewModel.NearestPoiId))
				Dispatcher.Dispatch(UpdatePins);
			if (e.PropertyName is nameof(MainViewModel.UserLatitude) or nameof(MainViewModel.UserLongitude))
				Dispatcher.Dispatch(UpdateMapUser);
			if (e.PropertyName == nameof(MainViewModel.IsTracking))
				TrackBtn.Text = _vm.IsTracking ? "Tat GPS" : "Bat GPS";
		};

		_vm.Pois.CollectionChanged += OnPoisChanged;
		StatusLabel.Text = _vm.StatusMessage;
		NearestLabel.Text = "Dang tim diem gan nhat...";

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
				StatusLabel.Text = "Loi tai ban do.";
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
		=> await _vm.ToggleTrackingCommand.ExecuteAsync(null);

	private void UpdateMapUser()
	{
		var sm = SphericalMercator.FromLonLat(_vm.UserLongitude, _vm.UserLatitude);
		StreetMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(sm.x, sm.y), 2);
		StreetMap.MyLocationLayer?.UpdateMyLocation(new Position(_vm.UserLatitude, _vm.UserLongitude), animated: true);
		StreetMap.RefreshGraphics();
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
			StatusLabel.Text = "Chua co POI de phat.";
			return;
		}

		try
		{
			StatusLabel.Text = $"Dang phat: {poi.ResolveName(_vm.SelectedLanguage)}";
			var heardSeconds = await _narration.PlayPoiAsync(poi, _vm.SelectedLanguage, _vm.ApiRootForAudio);
			StatusLabel.Text = $"Da phat xong ({heardSeconds}s): {poi.ResolveName(_vm.SelectedLanguage)}";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"Loi phat audio: {ex.Message}";
		}
	}
}
