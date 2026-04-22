using Microsoft.AspNetCore.SignalR;
using VinhKhanh.API.Services;

namespace VinhKhanh.API.Hubs;

public class VinhKhanhHub(IConnectionTracker tracker) : Hub
{
	public override Task OnConnectedAsync()
	{
		tracker.AddConnection(Context.ConnectionId);
		Console.WriteLine($"SignalR connected: {Context.ConnectionId}. Online: {tracker.GetOnlineCount()}");
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		tracker.RemoveConnection(Context.ConnectionId);
		Console.WriteLine($"SignalR disconnected: {Context.ConnectionId}. Online: {tracker.GetOnlineCount()}");
		return base.OnDisconnectedAsync(exception);
	}
}

