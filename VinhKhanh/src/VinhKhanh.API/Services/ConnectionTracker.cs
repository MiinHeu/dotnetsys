using System.Collections.Concurrent;

namespace VinhKhanh.API.Services;

public class ConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();

    public void AddConnection(string connectionId)
    {
        _connections.TryAdd(connectionId, 0);
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public int GetOnlineCount()
    {
        return _connections.Count*2;
    }
}
