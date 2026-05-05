using System.Collections.Specialized;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using Position = Mapsui.UI.Maui.Position;
using VinhKhanh.Shared.DTOs;
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
	private MemoryLayer? _tourLayer;

	public MainPage()
	{
		InitializeComponent();
		_vm = MauiProgram.Services.GetRequiredService<MainViewModel>();
		_narration = MauiProgram.Services.GetRequiredService<NarrationService>();
		BindingContext = _vm;

		LangPicker.ItemsSource = new[] { "vi", "en", "zh", "ko", "ja" };
		LangPicker.SelectedItem = _vm.SelectedLanguage;

		_vm.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MainViewModel.StatusMessage))
				Dispatcher.Dispatch(() => StatusLabel.Text = _vm.StatusMessage);
			if (e.PropertyName == nameof(MainViewModel.NearestLabel))
				Dispatcher.Dispatch(() => NearestLabel.Text = string.IsNullOrWhiteSpace(_vm.NearestLabel)
					? VinhKhanh.App.Resources.Strings.AppResources.StatusNearestPoiDefault
					: string.Format(VinhKhanh.App.Resources.Strings.AppResources.NearestLabelFormat, _vm.NearestLabel));
			if (e.PropertyName == nameof(MainViewModel.NearestPoiId))
				Dispatcher.Dispatch(UpdatePins);
			if (e.PropertyName is nameof(MainViewModel.UserLatitude) or nameof(MainViewModel.UserLongitude))
				Dispatcher.Dispatch(UpdateMapUser);
			if (e.PropertyName == nameof(MainViewModel.IsTracking))
				Dispatcher.Dispatch(() =>
					TrackBtn.Text = _vm.IsTracking ? VinhKhanh.App.Resources.Strings.AppResources.GpsButtonOff : VinhKhanh.App.Resources.Strings.AppResources.GpsButtonOn);
			if (e.PropertyName == nameof(MainViewModel.SelectedTour))
				Dispatcher.Dispatch(UpdateTourPath);
		};

		_vm.Pois.CollectionChanged += OnPoisChanged;
		StatusLabel.Text = _vm.StatusMessage;
		NearestLabel.Text = VinhKhanh.App.Resources.Strings.AppResources.StatusNearestPoiDefault;

		Loaded += async (_, _) =>
		{
			try
			{
                // Khởi tạo bản đồ an toàn khi trang đã nạp xong
                InitializeMap();
				CenterMap(DefaultLatitude, DefaultLongitude);
				await _vm.SyncPoisCommand.ExecuteAsync(null);
				UpdatePins();
			}
			catch (Exception ex)
			{
                System.Diagnostics.Debug.WriteLine($"[ERROR] MainPage Loaded: {ex}");
				StatusLabel.Text = VinhKhanh.App.Resources.Strings.AppResources.MapLoadingError;
			}
		};
	}

	private void InitializeMap()
	{
        if (StreetMap?.Map == null) return; // Kiểm tra an toàn

		try 
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
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] InitializeMap Error: {ex}");
		}
	}

	private void OnPoisChanged(object? sender, NotifyCollectionChangedEventArgs e)
		=> Dispatcher.Dispatch(UpdatePins);

	private void OnLangChanged(object? sender, EventArgs e)
	{
		if (LangPicker.SelectedItem is string lang && _vm.SelectedLanguage != lang)
		{
			_vm.SelectedLanguage = lang;
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, lang);
			
			var culture = new System.Globalization.CultureInfo(lang);
			System.Globalization.CultureInfo.CurrentCulture = culture;
			System.Globalization.CultureInfo.CurrentUICulture = culture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
			
			Application.Current!.MainPage = new AppShell();
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
			StatusLabel.Text = VinhKhanh.App.Resources.Strings.AppResources.NarrationStatusNoPoi;
			System.Diagnostics.Debug.WriteLine("[MainPage] No POI selected for playback");
			return;
		}

		try
		{
			StatusLabel.Text = string.Format(VinhKhanh.App.Resources.Strings.AppResources.NarrationStatusPlaying, poi.ResolveName(_vm.SelectedLanguage));
			var heardSeconds = await _narration.PlayPoiAsync(poi, _vm.SelectedLanguage, _vm.ApiRootForAudio);
			StatusLabel.Text = string.Format(VinhKhanh.App.Resources.Strings.AppResources.NarrationStatusFinished, heardSeconds, poi.ResolveName(_vm.SelectedLanguage));

			// Ghi log lượt nghe thủ công
			var visit = new VisitLogDto(poi.Id, MauiProgram.Services.GetRequiredService<SessionService>().SessionId, _vm.SelectedLanguage, "MANUAL", heardSeconds);
			var api = MauiProgram.Services.GetRequiredService<ApiClientService>();
			var outbox = MauiProgram.Services.GetRequiredService<IOutboxService>();
			if (!await api.TryPostAnalyticsVisitAsync(visit))
				await outbox.EnqueueVisitAsync(visit);
		}
		catch (Exception ex)
		{
			StatusLabel.Text = string.Format(VinhKhanh.App.Resources.Strings.AppResources.NarrationStatusError, ex.Message);
			System.Diagnostics.Debug.WriteLine($"[MainPage] Playback error: {ex}");
		}
	}

	private async void UpdateTourPath()
	{
		// 1. Don dep Layer cu
		if (_tourLayer != null)
		{
			StreetMap.Map.Layers.Remove(_tourLayer);
			_tourLayer = null;
		}

		var tour = _vm.SelectedTour;
		if (tour == null || tour.Stops == null || tour.Stops.Count < 2)
		{
			StreetMap.RefreshGraphics();
			return;
		}

		// Thu thập tọa độ các điểm dừng
		var waypoints = new List<(double Lon, double Lat)>();
		foreach (var stop in tour.Stops.OrderBy(s => s.StopOrder))
		{
			if (stop.Poi == null) continue;
			waypoints.Add((stop.Poi.Longitude, stop.Poi.Latitude));
		}

		if (waypoints.Count < 2) return;

		// 2. Gọi Routing API để vẽ đường đi bộ thực tế
		var routingService = MauiProgram.Services.GetRequiredService<RoutingService>();
		var routePoints = await routingService.GetWalkingRouteAsync(waypoints);

		var coordinates = new List<Coordinate>();
		var mapPoints = new List<Mapsui.MPoint>();

		// Nếu gọi API thất bại, fallback về đường chim bay (đường thẳng)
		var sourcePoints = routePoints ?? waypoints;

		foreach (var pt in sourcePoints)
		{
			var sm = SphericalMercator.FromLonLat(pt.Lon, pt.Lat);
			coordinates.Add(new Coordinate(sm.x, sm.y));
			mapPoints.Add(new Mapsui.MPoint(sm.x, sm.y));
		}

		var lineString = new NetTopologySuite.Geometries.LineString(coordinates.ToArray());

		// 3. Tao Layer moi voi Style mau cam
		var feature = new GeometryFeature { Geometry = lineString };
		_tourLayer = new MemoryLayer
		{
			Name = "SelectedTourPath",
			Features = new[] { feature },
			Style = new VectorStyle
			{
				Line = new Pen
				{
					Color = Mapsui.Styles.Color.Orange,
					Width = 5,
					PenStyle = PenStyle.Solid,
					PenStrokeCap = PenStrokeCap.Round
				}
			}
		};

		StreetMap.Map.Layers.Add(_tourLayer);
		StreetMap.RefreshGraphics();

		// 4. Tu dong Zoom de thay tron ven lo trinh
		if (mapPoints.Count > 0)
		{
			var minX = mapPoints.Min(p => p.X);
			var minY = mapPoints.Min(p => p.Y);
			var maxX = mapPoints.Max(p => p.X);
			var maxY = mapPoints.Max(p => p.Y);
			
			// Them padding 10%
			var dx = (maxX - minX) * 0.2;
			var dy = (maxY - minY) * 0.2;
			
			StreetMap.Map.Navigator.ZoomToBox(new Mapsui.MRect(minX - dx, minY - dy, maxX + dx, maxY + dy));
		}
	}
}
