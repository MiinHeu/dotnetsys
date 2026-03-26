using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Hubs;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class TourController(ApplicationDbContext db, IHubContext<VinhKhanhHub> hub) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] string lang = "vi")
	{
		var tours = await db.Tours
			.Include(t => t.Translations.Where(tr => tr.LanguageCode == lang))
			.Include(t => t.Stops.OrderBy(s => s.StopOrder))
				.ThenInclude(s => s.Poi)
					.ThenInclude(p => p.Translations.Where(pt => pt.LanguageCode == lang))
			.AsNoTracking()
			.ToListAsync();

		return Ok(tours);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id, [FromQuery] string lang = "vi")
	{
		var tour = await db.Tours
			.Include(t => t.Translations.Where(tr => tr.LanguageCode == lang))
			.Include(t => t.Stops.OrderBy(s => s.StopOrder))
				.ThenInclude(s => s.Poi)
					.ThenInclude(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id);

		return tour == null ? NotFound() : Ok(tour);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] TourCreateDto dto)
	{
		var tour = new Tour
		{
			Name = dto.Name,
			Description = dto.Description,
			EstimatedMinutes = dto.EstimatedMinutes,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		foreach (var stop in dto.Stops)
		{
			tour.Stops.Add(new TourStop
			{
				PoiId = stop.PoiId,
				StopOrder = stop.StopOrder,
				StayMinutes = stop.StayMinutes,
				Note = stop.Note
			});
		}

		db.Tours.Add(tour);
		await db.SaveChangesAsync();

		await hub.Clients.All.SendAsync("TourCreated", tour);
		return CreatedAtAction(nameof(GetById), new { id = tour.Id }, tour);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, [FromBody] TourCreateDto dto)
	{
		var tour = await db.Tours
			.Include(t => t.Stops)
			.FirstOrDefaultAsync(t => t.Id == id);

		if (tour == null) return NotFound();

		tour.Name = dto.Name;
		tour.Description = dto.Description;
		tour.EstimatedMinutes = dto.EstimatedMinutes;
		tour.UpdatedAt = DateTime.UtcNow;

		db.TourStops.RemoveRange(tour.Stops);
		tour.Stops.Clear();

		foreach (var stop in dto.Stops)
		{
			tour.Stops.Add(new TourStop
			{
				PoiId = stop.PoiId,
				StopOrder = stop.StopOrder,
				StayMinutes = stop.StayMinutes,
				Note = stop.Note
			});
		}

		await db.SaveChangesAsync();

		await hub.Clients.All.SendAsync("TourUpdated", tour);
		return Ok(tour);
	}
}

