using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanh.App.Services;

public record LocationUpdatedMessage(Location Location);

public class GpsService : IGpsService
{
	private CancellationTokenSource? _cts;

	public async Task StartTrackingAsync()
	{
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

	private async Task StartIosLoopAsync(CancellationToken ct)
	{
		// iOS: Info.plist UIBackgroundModes: location cho phép loop này chạy nền
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
		Platform.AppContext.StopService(new Android.Content.Intent(
			Platform.AppContext, typeof(Platforms.Android.GpsForegroundService)));
#endif
		return Task.CompletedTask;
	}
}
