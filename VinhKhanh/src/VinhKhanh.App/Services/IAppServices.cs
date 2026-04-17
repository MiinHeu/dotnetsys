using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

public interface IGpsService
{
	Task StartTrackingAsync();
	Task StopTrackingAsync();
}

public interface IGeofenceService
{
	Task<List<Poi>> CheckTriggeredAsync(Microsoft.Maui.Devices.Sensors.Location loc, List<Poi> pois);
}

public interface INarrationService
{
	Task EnqueueAsync(Poi poi, string language);
	void InterruptIfHigherPriority(int newPriority);
	Task StopCurrentAsync();
	Task PreFetchAsync(Models.PoiSnapshot poi, string language);
	bool IsPlaying { get; }
}
