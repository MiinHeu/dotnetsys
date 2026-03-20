using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure;
using VinhKhanh.Shared.DTOs;
using VinhKhanh.Shared.Models;

namespace VinhKhanh.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoiController(AppDbContext db) : ControllerBase
{
    // GET /api/poi?lang=vi
    [HttpGet]
    public async Task<ActionResult<List<Poi>>> GetAll([FromQuery] string lang = "vi")
    {
        var items = await db.Pois
            .Include(p => p.Translations.Where(t => t.LanguageCode == lang))
            .AsNoTracking()
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/poi/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Poi>> GetById(int id)
    {
        var poi = await db.Pois
            .Include(p => p.Translations)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return poi == null ? NotFound() : Ok(poi);
    }

    // POST /api/poi/nearby
    // Body: { "lat": ..., "lon": ... }
    [HttpPost("nearby")]
    public async Task<ActionResult<List<Poi>>> FindNearby([FromBody] LocationQueryDto loc)
    {
        // In PoC, we load in-memory and compute Haversine distance.
        // Can be optimized later with spatial indexes.
        var all = await db.Pois
            .Include(p => p.Translations)
            .AsNoTracking()
            .ToListAsync();

        var nearby = all
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

        return Ok(nearby);
    }

    // POST /api/poi
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Poi poi)
    {
        poi.CreatedAt = poi.UpdatedAt = DateTime.UtcNow;
        db.Pois.Add(poi);
        await db.SaveChangesAsync();

        return Ok(poi);
    }

    // PUT /api/poi/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] Poi updated)
    {
        var poi = await db.Pois.FindAsync(id);
        if (poi == null) return NotFound();

        poi.Name = updated.Name;
        poi.Description = updated.Description;
        poi.Latitude = updated.Latitude;
        poi.Longitude = updated.Longitude;
        poi.TriggerRadiusMeters = updated.TriggerRadiusMeters;
        poi.Priority = updated.Priority;
        poi.IsActive = updated.IsActive;
        poi.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(poi);
    }
}

