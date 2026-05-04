using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class SessionController(ApplicationDbContext db, ILogger<SessionController> logger) : ControllerBase
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
			IsReturning = isReturning
		};

		db.DeviceSessions.Add(session);

		try
		{
			await db.SaveChangesAsync(ct);
			logger.LogInformation("Session started: {SessionId}, Device={Model} ({Platform} {Os}), Returning={IsReturning}",
				dto.SessionId, dto.DeviceModel, dto.DevicePlatform, dto.OsVersion, isReturning);
			return Ok(new { message = "Session started", sessionDbId = session.Id, isReturning });
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

	private static string Truncate(string? value, int maxLength)
		=> string.IsNullOrWhiteSpace(value) ? "" : value.Trim().Length <= maxLength ? value.Trim() : value.Trim()[..maxLength];
}
