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
		poi.CreatedAt = poi.UpdatedAt = DateTime.UtcNow;
		// Defaults for safety.
		poi.IsActive = true;

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

		poi.Name = updated.Name;
		poi.Description = updated.Description;
		poi.Latitude = updated.Latitude;
		poi.Longitude = updated.Longitude;
		poi.MapX = updated.MapX;
		poi.MapY = updated.MapY;
		poi.TriggerRadiusMeters = updated.TriggerRadiusMeters;
		poi.Priority = updated.Priority;
		poi.IsActive = updated.IsActive;
		poi.AudioViUrl = updated.AudioViUrl;
		poi.ImageUrl = updated.ImageUrl;

		poi.UpdatedAt = DateTime.UtcNow;
		await db.SaveChangesAsync();

		await hub.Clients.All.SendAsync("PoiUpdated", poi);
		return Ok(poi);
	}

	[HttpPost("{id}/translation")]
	public async Task<IActionResult> AddTranslation(int id, [FromBody] PoiTranslationDto dto)
	{
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
		await db.SaveChangesAsync();
		return Ok();
	}
}
