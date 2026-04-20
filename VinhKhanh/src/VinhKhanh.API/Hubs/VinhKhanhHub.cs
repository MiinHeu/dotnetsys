using Microsoft.AspNetCore.SignalR;
using VinhKhanh.API.Services;

namespace VinhKhanh.API.Hubs;

public class VinhKhanhHub(IConnectionTracker tracker) : Hub
{
	public override Task OnConnectedAsync()
	{
		tracker.AddConnection(Context.ConnectionId);
		Console.WriteLine($"SignalR connected: {Context.ConnectionId}. Total: {tracker.GetConnectionCount()}");
		return base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		tracker.RemoveConnection(Context.ConnectionId);
		Console.WriteLine($"SignalR disconnected: {Context.ConnectionId}. Total: {tracker.GetConnectionCount()}");
		await base.OnDisconnectedAsync(exception);
	}
}

