namespace VinhKhanh.Infrastructure.Data;

public class AppUser
{
	public int Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public string Role { get; set; } = "Owner"; // Admin, Owner
	public bool IsActive { get; set; } = true;
	public int? OwnedPoiId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

