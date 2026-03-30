using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Hubs;
using VinhKhanh.API.Utilities;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class PoiController(ApplicationDbContext db, IHubContext<VinhKhanhHub> hub) : ControllerBase
{
	private static string? NormalizeQr(string? code)
	{
		if (string.IsNullOrWhiteSpace(code)) return null;
		return code.Trim().ToUpperInvariant();
	}

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] string lang = "vi")
	{
		var pois = await db.Pois
			.Include(p => p.Translations.Where(t => t.LanguageCode == lang))
			.AsNoTracking()
			.ToListAsync();

		return Ok(pois);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id)
	{
		var poi = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == id);

		return poi == null ? NotFound() : Ok(poi);
	}

	[HttpGet("qrcode/{code}")]
	public async Task<IActionResult> GetByQrCode(string code)
	{
		var key = NormalizeQr(code);
		if (string.IsNullOrEmpty(key)) return BadRequest("Ma QR khong hop le.");

		var poi = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.QrCode == key);

		return poi == null ? NotFound() : Ok(poi);
	}

	[HttpPost("nearby")]
	public async Task<IActionResult> FindNearby([FromBody] LocationQueryDto loc)
	{
		var pois = await db.Pois
			.Include(p => p.Translations.Where(t => t.LanguageCode == loc.Lang))
			.AsNoTracking()
			.ToListAsync();

		var items = pois
			.Select(p => new
			{
				Poi = p,
				Dist = GeoMath.Haversine(loc.Lat, loc.Lon, p.Latitude, p.Longitude)
			})
			.Where(x => x.Dist <= x.Poi.TriggerRadiusMeters)
			.OrderByDescending(x => x.Poi.Priority)
			.ThenBy(x => x.Dist)
			.Select(x => x.Poi)
			.ToList();

		return Ok(items);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] Poi poi)
	{
		poi.QrCode = NormalizeQr(poi.QrCode);
		if (!string.IsNullOrEmpty(poi.QrCode) && await db.Pois.AnyAsync(p => p.QrCode == poi.QrCode))
			return Conflict($"Ma QR '{poi.QrCode}' da ton tai.");

		poi.CreatedAt = poi.UpdatedAt = DateTime.UtcNow;
		// Defaults for safety.
		poi.IsActive = true;
		if (poi.ContentVersion < 1) poi.ContentVersion = 1;

		db.Pois.Add(poi);
		await db.SaveChangesAsync();

		await hub.Clients.All.SendAsync("PoiCreated", poi);
		return CreatedAtAction(nameof(GetById), new { id = poi.Id }, poi);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, [FromBody] Poi updated)
	{
		var poi = await db.Pois.FirstOrDefaultAsync(p => p.Id == id);
		if (poi == null) return NotFound();

		var newQr = NormalizeQr(updated.QrCode);
		if (newQr != poi.QrCode && !string.IsNullOrEmpty(newQr) &&
		    await db.Pois.AnyAsync(p => p.QrCode == newQr && p.Id != id))
			return Conflict($"Ma QR '{newQr}' da ton tai.");

		var contentChanged =
			poi.Name != updated.Name
			|| poi.Description != updated.Description
			|| poi.AudioViUrl != updated.AudioViUrl
			|| poi.ImageUrl != updated.ImageUrl
			|| poi.QrCode != newQr
			|| Math.Abs(poi.Latitude - updated.Latitude) > 1e-9
			|| Math.Abs(poi.Longitude - updated.Longitude) > 1e-9
			|| Math.Abs(poi.TriggerRadiusMeters - updated.TriggerRadiusMeters) > 1e-6
			|| poi.Priority != updated.Priority
			|| poi.CooldownSeconds != updated.CooldownSeconds
			|| poi.Category != updated.Category
			|| poi.OwnerInfo != updated.OwnerInfo;

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
		await db.SaveChangesAsync();

		await hub.Clients.All.SendAsync("PoiUpdated", poi);
		return Ok(poi);
	}

	// POST /api/poi/{id}/translation — upsert bản dịch (Translations.tsx)
	[HttpPost("{id:int}/translation")]
	public async Task<IActionResult> AddTranslation(int id, [FromBody] PoiTranslationDto dto)
	{
		var poi = await db.Pois.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
		if (poi == null) return NotFound();

		var existing = await db.PoiTranslations
			.FirstOrDefaultAsync(t => t.PoiId == id && t.LanguageCode == dto.LanguageCode);

		if (existing != null)
		{
			existing.Name = dto.Name;
			existing.Description = dto.Description;
			existing.AudioUrl = dto.AudioUrl;
		}
		else
		{
			db.PoiTranslations.Add(new PoiTranslation
			{
				PoiId = id,
				LanguageCode = dto.LanguageCode,
				Name = dto.Name,
				Description = dto.Description,
				AudioUrl = dto.AudioUrl
			});
		}

		var poiEntity = await db.Pois.FirstOrDefaultAsync(p => p.Id == id);
		if (poiEntity != null)
		{
			poiEntity.ContentVersion++;
			poiEntity.UpdatedAt = DateTime.UtcNow;
		}

		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Deactivate(int id)
	{
		var poi = await db.Pois.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
		if (poi == null) return NotFound();

		poi.IsActive = false;
		poi.UpdatedAt = DateTime.UtcNow;
		await db.SaveChangesAsync();
		await hub.Clients.All.SendAsync("PoiUpdated", poi);
		return NoContent();
	}
}

