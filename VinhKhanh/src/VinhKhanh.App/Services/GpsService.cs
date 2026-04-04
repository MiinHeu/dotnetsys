using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanh.App.Services;

public record LocationUpdatedMessage(Location Location);

public class GpsService : IGpsService
{
	private CancellationTokenSource? _cts;

	private static readonly (double lat, double lon)[] RouteWaypoints =
	[
		(10.7535, 106.6782),
		(10.7533, 106.6781),
		(10.7531, 106.6780),
		(10.7529, 106.6779),
		(10.7528, 106.6778),
	];

	public async Task StartTrackingAsync()
	{
		if (IsMockEnabled())
		{
			_cts = new CancellationTokenSource();
			_ = RunRouteSimulationAsync(_cts.Token);
			return;
		}

		if (DeviceInfo.Platform == DevicePlatform.Android)
		{
			var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
			if (status != PermissionStatus.Granted)
				await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

#if ANDROID
			var intent = new Android.Content.Intent(
				Platform.AppContext, typeof(Platforms.Android.GpsForegroundService));
			Platform.AppContext.StartForegroundService(intent);
#endif
		}
		else if (DeviceInfo.Platform == DevicePlatform.iOS)
		{
			var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
			if (status != PermissionStatus.Granted)
				await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

			_cts = new CancellationTokenSource();
			_ = StartIosLoopAsync(_cts.Token);
		}
	}

	private async Task RunRouteSimulationAsync(CancellationToken ct)
	{
		System.Diagnostics.Debug.WriteLine("[GPS-MOCK] Route simulation bắt đầu.");
		var i = 0;
		while (!ct.IsCancellationRequested)
		{
			var (lat, lon) = RouteWaypoints[i];
			Publish(lat, lon);
			System.Diagnostics.Debug.WriteLine($"[GPS-MOCK] Step {i}/{RouteWaypoints.Length - 1} → ({lat:F6}, {lon:F6})");

			i = (i + 1) % RouteWaypoints.Length;

			try { await Task.Delay(1500, ct).ConfigureAwait(false); }
			catch (OperationCanceledException) { break; }
		}
	}

	private static void Publish(double lat, double lon)
		=> WeakReferenceMessenger.Default.Send(new LocationUpdatedMessage(new Location(lat, lon)
		{
			Accuracy = 5,
			Timestamp = DateTimeOffset.UtcNow
		}));

	private async Task StartIosLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var loc = await Geolocation.Default.GetLocationAsync(
					new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), ct);
				if (loc != null)
					WeakReferenceMessenger.Default.Send(new LocationUpdatedMessage(loc));
			}
			catch (OperationCanceledException) { break; }
			catch { /* ignore GPS errors */ }

			await Task.Delay(3000, ct).ConfigureAwait(false);
		}
	}

	public Task StopTrackingAsync()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;

#if ANDROID
		if (!IsMockEnabled())
		{
			Platform.AppContext.StopService(new Android.Content.Intent(
				Platform.AppContext, typeof(Platforms.Android.GpsForegroundService)));
		}
#endif
		return Task.CompletedTask;
	}

	private static bool IsMockEnabled()
		=> Microsoft.Maui.Storage.Preferences.Get(AppPreferences.MockGpsEnabled, false);
}
