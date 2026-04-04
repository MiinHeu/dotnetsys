using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using VinhKhanh.App.Services;
using VinhKhanh.App.ViewModels;
using ZXing.Net.Maui.Controls;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace VinhKhanh.App;

public static class MauiProgram
{
	public static IServiceProvider Services { get; private set; } = default!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
			.UseMauiApp<App>()
			.UseBarcodeReader()
			.UseSkiaSharp() // Dùng SkiaSharp để Mapsui siêu mượt
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.AddAudio();


		// ── Services ──────────────────────────────────────────────────────────
		builder.Services.AddSingleton<ApiClientService>();
		builder.Services.AddSingleton<LocalPoiCacheService>();       // cache cũ — giữ nguyên
		builder.Services.AddSingleton<ILocalDbService, LocalDbService>(); // FIX #1: LocalTour
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddSingleton<GeofenceCooldownStore>();
		builder.Services.AddSingleton<IOutboxService, OutboxService>();

		// GPS & Geofence — FIX #4, #5
		builder.Services.AddSingleton<IGpsService, GpsService>();
		builder.Services.AddSingleton<IGeofenceService, GeofenceService>();

		// Narration — FIX #2: INarrationService (không phải INarrationEngine)
		builder.Services.AddSingleton<NarrationService>();           // concrete singleton
		builder.Services.AddSingleton<INarrationService>(sp =>      // alias interface → same instance
			sp.GetRequiredService<NarrationService>());


		// Connectivity — FIX Gap 5: inject vào App.xaml.cs constructor
		builder.Services.AddSingleton<ConnectivityService>();

		// ── ViewModels ────────────────────────────────────────────────────────
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<ToursViewModel>();
		builder.Services.AddSingleton<ChatViewModel>();
		builder.Services.AddTransient<PoiDetailViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// ── Pages ─────────────────────────────────────────────────────────────
		builder.Services.AddTransient<QrScanPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<ToursPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		Services = app.Services;
		return app;
	}
}
