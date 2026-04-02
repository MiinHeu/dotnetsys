using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Hubs;
using VinhKhanh.API.Utilities;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class PoiController(
	ApplicationDbContext db,
	IHubContext<VinhKhanhHub> hub) : ControllerBase
{
	private static string? NormalizeQr(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
			return null;

		return code.Trim().ToUpperInvariant();
	}

	private static string NormalizeLang(string? lang)
		=> string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLowerInvariant();

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] string lang = "vi", CancellationToken ct = default)
	{
		_ = NormalizeLang(lang);

		var pois = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.OrderByDescending(p => p.Priority)
			.ThenBy(p => p.Name)
			.ToListAsync(ct);

		return Ok(pois);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
	{
		var poi = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == id, ct);

		return poi == null ? NotFound() : Ok(poi);
	}

	[HttpGet("qrcode/{code}")]
	public async Task<IActionResult> GetByQrCode(string code, CancellationToken ct = default)
	{
		var key = NormalizeQr(code);
		if (string.IsNullOrEmpty(key))
			return BadRequest(new { message = "Ma QR khong hop le." });

		var poi = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.QrCode == key, ct);

		return poi == null ? NotFound() : Ok(poi);
	}

	[HttpPost("nearby")]
	public async Task<IActionResult> FindNearby([FromBody] LocationQueryDto loc, CancellationToken ct = default)
	{
		if (!IsValidCoordinate(loc.Lat, loc.Lon))
			return BadRequest(new { message = "Toa do khong hop le." });

		var pois = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.ToListAsync(ct);

		var items = pois
			.Select(p => new
			{
				Poi = p,
				Dist = GeoMath.Haversine(loc.Lat, loc.Lon, p.Latitude, p.Longitude)
			})
			.Where(x => x.Poi.TriggerRadiusMeters > 0 && x.Dist <= x.Poi.TriggerRadiusMeters)
			.OrderByDescending(x => x.Poi.Priority)
			.ThenBy(x => x.Dist)
			.Select(x => x.Poi)
			.ToList();

		return Ok(items);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] Poi poi, CancellationToken ct = default)
	{
		TrimPoiFields(poi);
		if (!TryValidatePoi(poi, out var error))
			return BadRequest(new { message = error });

		poi.QrCode = NormalizeQr(poi.QrCode);
		if (!string.IsNullOrEmpty(poi.QrCode)
		    && await db.Pois.IgnoreQueryFilters().AnyAsync(p => p.QrCode == poi.QrCode, ct))
		{
			return Conflict(new { message = $"Ma QR '{poi.QrCode}' da ton tai." });
		}

		NormalizeTranslations(poi.Translations);
		poi.CreatedAt = DateTime.UtcNow;
		poi.UpdatedAt = DateTime.UtcNow;
		poi.IsActive = true;
		if (poi.ContentVersion < 1) poi.ContentVersion = 1;

		db.Pois.Add(poi);
		await db.SaveChangesAsync(ct);

		await hub.Clients.All.SendAsync("PoiCreated", poi, ct);
		return CreatedAtAction(nameof(GetById), new { id = poi.Id }, poi);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, [FromBody] Poi updated, CancellationToken ct = default)
	{
		TrimPoiFields(updated);
		if (!TryValidatePoi(updated, out var error))
			return BadRequest(new { message = error });

		var poi = await db.Pois
			.Include(p => p.Translations)
			.FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		var newQr = NormalizeQr(updated.QrCode);
		if (newQr != poi.QrCode
		    && !string.IsNullOrEmpty(newQr)
		    && await db.Pois.IgnoreQueryFilters().AnyAsync(p => p.QrCode == newQr && p.Id != id, ct))
		{
			return Conflict(new { message = $"Ma QR '{newQr}' da ton tai." });
		}

		var contentChanged =
			poi.Name != updated.Name
			|| poi.Description != updated.Description
			|| poi.OwnerInfo != updated.OwnerInfo
			|| poi.AudioViUrl != updated.AudioViUrl
			|| poi.ImageUrl != updated.ImageUrl
			|| poi.QrCode != newQr
			|| Math.Abs(poi.Latitude - updated.Latitude) > 1e-9
			|| Math.Abs(poi.Longitude - updated.Longitude) > 1e-9
			|| Math.Abs(poi.MapX - updated.MapX) > 1e-6
			|| Math.Abs(poi.MapY - updated.MapY) > 1e-6
			|| Math.Abs(poi.TriggerRadiusMeters - updated.TriggerRadiusMeters) > 1e-6
			|| poi.Priority != updated.Priority
			|| poi.CooldownSeconds != updated.CooldownSeconds
			|| poi.Category != updated.Category
			|| poi.IsActive != updated.IsActive;

		poi.Name = updated.Name;
		poi.Description = updated.Description;
		poi.OwnerInfo = updated.OwnerInfo;
		poi.Latitude = updated.Latitude;
		poi.Longitude = updated.Longitude;
		poi.MapX = updated.MapX;
		poi.MapY = updated.MapY;
		poi.TriggerRadiusMeters = updated.TriggerRadiusMeters;
		poi.Priority = updated.Priority;
		poi.CooldownSeconds = updated.CooldownSeconds;
		poi.Category = updated.Category;
		poi.IsActive = updated.IsActive;
		poi.AudioViUrl = updated.AudioViUrl;
		poi.ImageUrl = updated.ImageUrl;
		poi.QrCode = newQr;
		if (contentChanged) poi.ContentVersion++;

		poi.UpdatedAt = DateTime.UtcNow;
		NormalizeTranslations(poi.Translations);

		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("PoiUpdated", poi, ct);
		return Ok(poi);
	}

	[HttpPost("{id:int}/translation")]
	public async Task<IActionResult> AddTranslation(int id, [FromBody] PoiTranslationDto dto, CancellationToken ct = default)
	{
		var lang = NormalizeLang(dto.LanguageCode);
		if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
			return BadRequest(new { message = "Name va Description ban dich khong duoc trong." });

		var poi = await db.Pois.FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		var existing = await db.PoiTranslations
			.FirstOrDefaultAsync(t => t.PoiId == id && t.LanguageCode == lang, ct);

		if (existing != null)
		{
			existing.Name = dto.Name.Trim();
			existing.Description = dto.Description.Trim();
			existing.AudioUrl = string.IsNullOrWhiteSpace(dto.AudioUrl) ? null : dto.AudioUrl.Trim();
		}
		else
		{
			db.PoiTranslations.Add(new PoiTranslation
			{
				PoiId = id,
				LanguageCode = lang,
				Name = dto.Name.Trim(),
				Description = dto.Description.Trim(),
				AudioUrl = string.IsNullOrWhiteSpace(dto.AudioUrl) ? null : dto.AudioUrl.Trim()
			});
		}

		poi.ContentVersion++;
		poi.UpdatedAt = DateTime.UtcNow;

		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("PoiUpdated", poi, ct);
		return Ok(new { message = "Translation saved", poiId = id, languageCode = lang });
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Deactivate(int id, CancellationToken ct = default)
	{
		var poi = await db.Pois.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		if (!poi.IsActive)
			return NoContent();

		poi.IsActive = false;
		poi.ContentVersion++;
		poi.UpdatedAt = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("PoiUpdated", poi, ct);
		return NoContent();
	}

	private static bool TryValidatePoi(Poi poi, out string error)
	{
		error = string.Empty;

		if (string.IsNullOrWhiteSpace(poi.Name))
		{
			error = "Ten POI khong duoc de trong.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(poi.Description))
		{
			error = "Mo ta POI khong duoc de trong.";
			return false;
		}

		if (!IsValidCoordinate(poi.Latitude, poi.Longitude))
		{
			error = "Toa do lat/lon khong hop le.";
			return false;
		}

		if (poi.MapX < 0 || poi.MapX > 100 || poi.MapY < 0 || poi.MapY > 100)
		{
			error = "MapX/MapY phai trong khoang 0..100.";
			return false;
		}

		if (poi.TriggerRadiusMeters <= 0 || poi.TriggerRadiusMeters > 1000)
		{
			error = "Ban kinh kich hoat phai lon hon 0 va <= 1000m.";
			return false;
		}

		if (poi.CooldownSeconds < 0 || poi.CooldownSeconds > 7200)
		{
			error = "CooldownSeconds phai trong khoang 0..7200.";
			return false;
		}

		if (poi.Priority < 0 || poi.Priority > 1000)
		{
			error = "Priority phai trong khoang 0..1000.";
			return false;
		}

		return true;
	}

	private static bool IsValidCoordinate(double lat, double lon)
		=> lat is >= -90 and <= 90 && lon is >= -180 and <= 180;

	private static void TrimPoiFields(Poi poi)
	{
		poi.Name = poi.Name?.Trim() ?? string.Empty;
		poi.Description = poi.Description?.Trim() ?? string.Empty;
		poi.OwnerInfo = string.IsNullOrWhiteSpace(poi.OwnerInfo) ? null : poi.OwnerInfo.Trim();
		poi.ImageUrl = string.IsNullOrWhiteSpace(poi.ImageUrl) ? null : poi.ImageUrl.Trim();
		poi.AudioViUrl = string.IsNullOrWhiteSpace(poi.AudioViUrl) ? null : poi.AudioViUrl.Trim();
	}

	private static void NormalizeTranslations(IEnumerable<PoiTranslation>? translations)
	{
		if (translations == null) return;
		foreach (var t in translations)
		{
			t.LanguageCode = NormalizeLang(t.LanguageCode);
			t.Name = t.Name?.Trim() ?? string.Empty;
			t.Description = t.Description?.Trim() ?? string.Empty;
			t.AudioUrl = string.IsNullOrWhiteSpace(t.AudioUrl) ? null : t.AudioUrl.Trim();
		}
	}
}
