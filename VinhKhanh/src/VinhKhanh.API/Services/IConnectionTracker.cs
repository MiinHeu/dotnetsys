namespace VinhKhanh.API.Services;

public interface IConnectionTracker
{
    void AddConnection(string connectionId);
    void RemoveConnection(string connectionId);
    int GetOnlineCount();
}
