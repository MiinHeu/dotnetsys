#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.App.Models;

namespace VinhKhanh.App.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeLocation, Exported = false)]
[System.Runtime.Versioning.SupportedOSPlatform("android26.0")]
public class GpsForegroundService : Service
{
	private const string CHANNEL_ID = "VinhKhanhGps";
	private const int NOTIFICATION_ID = 1001;
	private CancellationTokenSource? _cts;

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand(
		Intent? intent, StartCommandFlags flags, int startId)
	{
		CreateNotificationChannel();
		var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
			.SetContentTitle("Phố Vĩnh Khánh")
			.SetContentText("Đang theo dõi vị trí để phát thuyết minh")
			.SetSmallIcon(Resource.Drawable.notification_icon_background)
			.SetOngoing(true)
			.Build();
		// Fix NoSuchMethodError: Only use the 3-parameter StartForeground on Android 10 (API 29) and above.
		// Fallback to the 2-parameter StartForeground on Android 8 and 9 (like Samsung J8).
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
		{
			StartForeground(NOTIFICATION_ID, notification, ForegroundService.TypeLocation);
		}
		else
		{
			StartForeground(NOTIFICATION_ID, notification);
		}
		_cts = new CancellationTokenSource();
		Task.Run(() => TrackingLoopAsync(_cts.Token));

		return StartCommandResult.Sticky;
	}

	private async Task TrackingLoopAsync(CancellationToken ct)
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
			catch (Exception ex) when (ex is not System.OperationCanceledException)
			{
				await Task.Delay(1000, ct).ConfigureAwait(false);
			}
			await Task.Delay(1500, ct).ConfigureAwait(false);
		}
	}

	public override void OnDestroy()
	{
		_cts?.Cancel();
		StopForeground(StopForegroundFlags.Remove);
		base.OnDestroy();
	}

	private void CreateNotificationChannel()
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
		var channel = new NotificationChannel(
			CHANNEL_ID, "GPS Tracking", NotificationImportance.Low);
		((NotificationManager?)GetSystemService(NotificationService))!
			.CreateNotificationChannel(channel);
	}
}
#endif
