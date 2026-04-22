using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace VinhKhanh.App.Services;

/// <summary>
/// Service to maintain a persistent SignalR connection to the backend hub
/// for real-time tracking of active devices.
/// </summary>
public sealed class RealtimeService : IAsyncDisposable
{
	private HubConnection? _hubConnection;
	private readonly SessionService _session;
	private bool _isStarted = false;

	public RealtimeService(SessionService session)
	{
		_session = session;
		Connectivity.ConnectivityChanged += OnConnectivityChanged;
	}

	public async Task StartAsync()
	{
		if (_isStarted) return;
		_isStarted = true;

		var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
		var hubUrl = $"{apiRoot}/hubs/vinh-khanh";

		_hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl)
			.WithAutomaticReconnect(new[] 
			{
				TimeSpan.Zero, 
				TimeSpan.FromSeconds(2), 
				TimeSpan.FromSeconds(5), 
				TimeSpan.FromSeconds(10), 
				TimeSpan.FromSeconds(30)
			})
			.Build();

		_hubConnection.Closed += async (error) =>
		{
			Debug.WriteLine($"[SignalR] Connection closed. Error: {error?.Message}");
			await Task.Delay(Random.Shared.Next(1000, 5000));
			await ConnectWithRetryAsync();
		};

		_hubConnection.Reconnecting += error =>
		{
			Debug.WriteLine($"[SignalR] Reconnecting... Error: {error?.Message}");
			return Task.CompletedTask;
		};

		_hubConnection.Reconnected += connectionId =>
		{
			Debug.WriteLine($"[SignalR] Reconnected. Connection ID: {connectionId}");
			return Task.CompletedTask;
		};

		await ConnectWithRetryAsync();
	}

	private async Task ConnectWithRetryAsync()
	{
		if (_hubConnection == null || _hubConnection.State == HubConnectionState.Connected)
			return;

		try
		{
			if (Connectivity.NetworkAccess != NetworkAccess.Internet)
			{
				Debug.WriteLine("[SignalR] No internet access. Waiting for connection...");
				return;
			}

			Debug.WriteLine("[SignalR] Attempting to connect...");
			await _hubConnection.StartAsync();
			Debug.WriteLine($"[SignalR] Connected successfully. ID: {_hubConnection.ConnectionId}");
			
			// Optional: Notify the server about our session
			// await _hubConnection.InvokeAsync("IdentifySession", _session.SessionId);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SignalR] Failed to connect: {ex.Message}");
		}
	}

	private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		if (e.NetworkAccess == NetworkAccess.Internet && _hubConnection?.State == HubConnectionState.Disconnected)
		{
			Debug.WriteLine("[SignalR] Internet restored. Reconnecting...");
			await ConnectWithRetryAsync();
		}
	}

	public async ValueTask DisposeAsync()
	{
		Connectivity.ConnectivityChanged -= OnConnectivityChanged;
		if (_hubConnection != null)
		{
			await _hubConnection.StopAsync();
			await _hubConnection.DisposeAsync();
			_hubConnection = null;
		}
	}
}
