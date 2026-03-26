namespace VinhKhanh.App.Services;

public sealed class SessionService
{
	public SessionService()
	{
		var id = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.SessionId, null);
		if (string.IsNullOrWhiteSpace(id))
		{
			id = Guid.NewGuid().ToString("N");
			Microsoft.Maui.Storage.Preferences.Set(AppPreferences.SessionId, id);
		}
		SessionId = id;
	}

	public string SessionId { get; }
}
