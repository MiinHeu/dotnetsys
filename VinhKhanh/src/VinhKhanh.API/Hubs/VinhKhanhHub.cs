using Microsoft.AspNetCore.SignalR;

namespace VinhKhanh.API.Hubs;

public class VinhKhanhHub : Hub
{
	// Events:
	// - PoiCreated(poi), PoiUpdated(poi)
	// - TourCreated(tour), TourUpdated(tour)
	public override Task OnConnectedAsync()
	{
		// Helpful for debugging SignalR connectivity.
		Console.WriteLine($"SignalR connected: {Context.ConnectionId}");
		return base.OnConnectedAsync();
	}
}

