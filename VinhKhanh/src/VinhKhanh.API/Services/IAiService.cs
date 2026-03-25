namespace VinhKhanh.API.Services;

using VinhKhanh.Shared.DTOs;

public interface IAiService
{
	Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null);
}

