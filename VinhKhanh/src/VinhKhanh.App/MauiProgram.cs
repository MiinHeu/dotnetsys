using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using VinhKhanh.App.Services;
using VinhKhanh.App.ViewModels;
using ZXing.Net.Maui.Controls;

namespace VinhKhanh.App;

public static class MauiProgram
{
	public static IServiceProvider Services { get; private set; } = default!;

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
			.UseMauiApp<App>()
			.UseBarcodeReader()
			.UseMauiMaps()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.AddAudio();

		builder.Services.AddSingleton<ApiClientService>();
		builder.Services.AddSingleton<LocalPoiCacheService>();
		builder.Services.AddSingleton<SessionService>();
		builder.Services.AddSingleton<GeofenceCooldownStore>();
		builder.Services.AddSingleton<NarrationService>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<ToursViewModel>();
		builder.Services.AddSingleton<ChatViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		Services = app.Services;
		return app;
	}
}
