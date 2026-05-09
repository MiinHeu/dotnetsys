using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class SessionController(
	ApplicationDbContext db, 
	ILogger<SessionController> logger,
	IHttpClientFactory httpClientFactory) : ControllerBase
{
	/// <summary>
	/// App gọi khi mở — tạo record phiên mới với thông tin thiết bị.
	/// Nếu SessionId đã từng tồn tại (cài lại app, mở lại sau vài ngày), đánh dấu IsReturning.
	/// </summary>
	[HttpPost("start")]
	public async Task<IActionResult> Start([FromBody] SessionStartDto dto, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		// Lấy IP của người dùng (Xử lý cả khi chạy qua Proxy/Azure)
		var ip = GetClientIp();

		// Kiểm tra xem du khách này đã từng dùng App trước đó chưa
		var isReturning = await db.DeviceSessions
			.AnyAsync(s => s.SessionId == dto.SessionId, ct);

		var session = new DeviceSession
		{
			SessionId = dto.SessionId.Trim(),
			DeviceModel = Truncate(dto.DeviceModel, 128),
			DevicePlatform = Truncate(dto.DevicePlatform, 32),
			OsVersion = Truncate(dto.OsVersion, 32),
			AppVersion = Truncate(dto.AppVersion, 32),
			Manufacturer = Truncate(dto.Manufacturer, 64),
			LanguageUsed = string.IsNullOrWhiteSpace(dto.LanguageUsed) ? "vi" : dto.LanguageUsed.Trim().ToLowerInvariant(),
			StartedAt = DateTime.UtcNow,
			LastHeartbeatAt = DateTime.UtcNow,
			IsReturning = isReturning,
			IpAddress = ip
		};

		// Tự động lấy vị trí từ IP (Không làm treo request chính nếu lỗi)
		try
		{
			if (!string.IsNullOrEmpty(ip) && ip != "::1" && ip != "127.0.0.1")
			{
				using var client = httpClientFactory.CreateClient();
				client.Timeout = TimeSpan.FromSeconds(3);
				var geo = await client.GetFromJsonAsync<IpGeoInfo>($"http://ip-api.com/json/{ip}?fields=status,country,city", ct);
				if (geo?.status == "success")
				{
					session.Country = geo.country;
					session.City = geo.city;
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning("Khong the lay thong tin vi tri cho IP {Ip}: {Error}", ip, ex.Message);
		}

		db.DeviceSessions.Add(session);

		try
		{
			await db.SaveChangesAsync(ct);
			logger.LogInformation("Session started: {SessionId} from {City}, {Country} (IP: {Ip})",
				dto.SessionId, session.City ?? "Unknown", session.Country ?? "Unknown", ip);
			return Ok(new { message = "Session started", sessionDbId = session.Id, isReturning, country = session.Country });
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	/// <summary>
	/// App gọi mỗi 60 giây — cập nhật trạng thái phiên (còn sống, số POI đã ghé, quãng đường).
	/// </summary>
	[HttpPost("heartbeat")]
	public async Task<IActionResult> Heartbeat([FromBody] SessionHeartbeatDto dto, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		// Tìm phiên gần nhất (chưa đóng) của SessionId này
		var session = await db.DeviceSessions
			.Where(s => s.SessionId == dto.SessionId && s.EndedAt == null)
			.OrderByDescending(s => s.StartedAt)
			.FirstOrDefaultAsync(ct);

		if (session == null)
			return NotFound(new { message = "Khong tim thay phien dang hoat dong." });

		session.LastHeartbeatAt = DateTime.UtcNow;
		session.PoisVisited = Math.Max(session.PoisVisited, dto.PoisVisited);
		session.DistanceMeters = Math.Max(session.DistanceMeters, dto.DistanceMeters);

		try
		{
			await db.SaveChangesAsync(ct);
			return Ok(new { message = "Heartbeat received" });
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	/// <summary>
	/// App gọi khi đóng app hoặc chuyển sang background — đánh dấu kết thúc phiên.
	/// </summary>
	[HttpPost("end")]
	public async Task<IActionResult> End([FromBody] SessionEndDto dto, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		var session = await db.DeviceSessions
			.Where(s => s.SessionId == dto.SessionId && s.EndedAt == null)
			.OrderByDescending(s => s.StartedAt)
			.FirstOrDefaultAsync(ct);

		if (session == null)
			return NotFound(new { message = "Khong tim thay phien dang hoat dong." });

		session.EndedAt = DateTime.UtcNow;
		session.LastHeartbeatAt = DateTime.UtcNow;
		session.PoisVisited = Math.Max(session.PoisVisited, dto.PoisVisited);
		session.DistanceMeters = Math.Max(session.DistanceMeters, dto.DistanceMeters);

		try
		{
			await db.SaveChangesAsync(ct);
			var durationMinutes = (session.EndedAt.Value - session.StartedAt).TotalMinutes;
			logger.LogInformation("Session ended: {SessionId}, Duration={Duration}min, POIs={Pois}, Distance={Dist}m",
				dto.SessionId, Math.Round(durationMinutes, 1), session.PoisVisited, Math.Round(session.DistanceMeters));
			return Ok(new { message = "Session ended", durationMinutes = Math.Round(durationMinutes, 1) });
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	private string GetClientIp()
	{
		// Thử lấy từ header X-Forwarded-For (Azure/Proxy)
		if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
		{
			return forwarded.ToString().Split(',')[0].Trim();
		}
		return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
	}

	private static string Truncate(string? value, int maxLength)
		=> string.IsNullOrWhiteSpace(value) ? "" : value.Trim().Length <= maxLength ? value.Trim() : value.Trim()[..maxLength];

	private class IpGeoInfo
	{
		public string? status { get; set; }
		public string? country { get; set; }
		public string? city { get; set; }
	}
}
