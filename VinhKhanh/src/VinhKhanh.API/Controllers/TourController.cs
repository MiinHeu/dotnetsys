using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using VinhKhanh.API.Hubs;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class TourController(
	ApplicationDbContext db,
	IHubContext<VinhKhanhHub> hub) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] string lang = "vi", CancellationToken ct = default)
	{
		_ = NormalizeLang(lang);

		var tours = await db.Tours
			.Where(t => t.IsActive)
			.Include(t => t.Translations)
			.Include(t => t.Stops.OrderBy(s => s.StopOrder))
				.ThenInclude(s => s.Poi)
					.ThenInclude(p => p.Translations)
			.AsNoTracking()
			.ToListAsync(ct);

		return Ok(tours);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id, [FromQuery] string lang = "vi", CancellationToken ct = default)
	{
		_ = NormalizeLang(lang);

		var tour = await db.Tours
			.Where(t => t.IsActive)
			.Include(t => t.Translations)
			.Include(t => t.Stops.OrderBy(s => s.StopOrder))
				.ThenInclude(s => s.Poi)
					.ThenInclude(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == id, ct);

		return tour == null ? NotFound() : Ok(tour);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Create([FromBody] TourCreateDto dto, CancellationToken ct = default)
	{
		if (!TryValidateTour(dto, out var error))
			return BadRequest(new { message = error });

		var poiIds = dto.Stops.Select(s => s.PoiId).Distinct().ToList();
		var existingPoiIds = await db.Pois
			.Where(p => poiIds.Contains(p.Id))
			.Select(p => p.Id)
			.ToListAsync(ct);

		var missing = poiIds.Except(existingPoiIds).ToList();
		if (missing.Count > 0)
			return BadRequest(new { message = $"POI khong ton tai hoac da vo hieu: {string.Join(",", missing)}" });

		var now = DateTime.UtcNow;
		var tour = new Tour
		{
			Name = dto.Name.Trim(),
			Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
			EstimatedMinutes = dto.EstimatedMinutes,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};

		foreach (var stop in dto.Stops.OrderBy(s => s.StopOrder))
		{
			tour.Stops.Add(new TourStop
			{
				PoiId = stop.PoiId,
				StopOrder = stop.StopOrder,
				StayMinutes = stop.StayMinutes,
				Note = string.IsNullOrWhiteSpace(stop.Note) ? null : stop.Note.Trim()
			});
		}

		db.Tours.Add(tour);
		await db.SaveChangesAsync(ct);

		await hub.Clients.All.SendAsync("TourCreated", tour, ct);
		return CreatedAtAction(nameof(GetById), new { id = tour.Id }, tour);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Update(int id, [FromBody] TourCreateDto dto, CancellationToken ct = default)
	{
		if (!TryValidateTour(dto, out var error))
			return BadRequest(new { message = error });

		var tour = await db.Tours
			.Include(t => t.Stops)
			.FirstOrDefaultAsync(t => t.Id == id, ct);

		if (tour == null)
			return NotFound();

		var poiIds = dto.Stops.Select(s => s.PoiId).Distinct().ToList();
		var existingPoiIds = await db.Pois
			.Where(p => poiIds.Contains(p.Id))
			.Select(p => p.Id)
			.ToListAsync(ct);

		var missing = poiIds.Except(existingPoiIds).ToList();
		if (missing.Count > 0)
			return BadRequest(new { message = $"POI khong ton tai hoac da vo hieu: {string.Join(",", missing)}" });

		tour.Name = dto.Name.Trim();
		tour.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
		tour.EstimatedMinutes = dto.EstimatedMinutes;
		tour.UpdatedAt = DateTime.UtcNow;

		// Merge logic: Dong bo danh sach TourStops
		// 1. Xoa cac stop khong con trong danh sach moi
		var newPoiIds = dto.Stops.Select(s => s.PoiId).ToList();
		var toRemove = tour.Stops.Where(old => !newPoiIds.Contains(old.PoiId)).ToList();
		foreach (var r in toRemove)
		{
			db.TourStops.Remove(r);
		}

		// 2. Cap nhat cac stop hien co hoac them moi
		foreach (var stopDto in dto.Stops.OrderBy(s => s.StopOrder))
		{
			var existing = tour.Stops.FirstOrDefault(s => s.PoiId == stopDto.PoiId);
			if (existing != null)
			{
				// Cap nhat thong tin stop hien co
				existing.StopOrder = stopDto.StopOrder;
				existing.StayMinutes = stopDto.StayMinutes;
				existing.Note = string.IsNullOrWhiteSpace(stopDto.Note) ? null : stopDto.Note.Trim();
			}
			else
			{
				// Them moi neu chua co
				tour.Stops.Add(new TourStop
				{
					PoiId = stopDto.PoiId,
					StopOrder = stopDto.StopOrder,
					StayMinutes = stopDto.StayMinutes,
					Note = string.IsNullOrWhiteSpace(stopDto.Note) ? null : stopDto.Note.Trim()
				});
			}
		}

		await db.SaveChangesAsync(ct);

		await hub.Clients.All.SendAsync("TourUpdated", tour, ct);
		return Ok(tour);
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Deactivate(int id, CancellationToken ct = default)
	{
		var tour = await db.Tours.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, ct);
		if (tour == null)
			return NotFound();

		if (!tour.IsActive)
			return NoContent();

		tour.IsActive = false;
		tour.UpdatedAt = DateTime.UtcNow;
		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("TourUpdated", tour, ct);
		return NoContent();
	}

	[HttpPost("{id:int}/translation")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> AddTranslation(int id, [FromBody] TourTranslationDto dto, CancellationToken ct = default)
	{
		var tour = await db.Tours.Include(t => t.Translations).FirstOrDefaultAsync(t => t.Id == id, ct);
		if (tour == null) return NotFound(new { message = "Tour khong ton tai." });

		var langCode = NormalizeLang(dto.LanguageCode);
		if (tour.Translations.Any(t => t.LanguageCode == langCode))
			return Conflict(new { message = $"Ban dich tieng '{langCode}' da ton tai." });

		if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
			return BadRequest(new { message = "Name va Description khong duoc de trong." });

		var trans = new TourTranslation
		{
			LanguageCode = langCode,
			Name = dto.Name.Trim(),
			Description = dto.Description.Trim(),
			TourId = id
		};

		db.TourTranslations.Add(trans);
		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("TourUpdated", tour, ct);
		return Ok(trans);
	}

	[HttpPut("{id:int}/translation/{lang}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UpdateTranslation(int id, string lang, [FromBody] TourTranslationDto dto, CancellationToken ct = default)
	{
		var langCode = NormalizeLang(lang);
		var trans = await db.TourTranslations.FirstOrDefaultAsync(t => t.TourId == id && t.LanguageCode == langCode, ct);
		if (trans == null) return NotFound(new { message = "Ban dich khong ton tai." });

		if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
			return BadRequest(new { message = "Name va Description khong duoc de trong." });

		trans.Name = dto.Name.Trim();
		trans.Description = dto.Description.Trim();

		await db.SaveChangesAsync(ct);
		
		var tour = await db.Tours.Include(t => t.Translations).FirstOrDefaultAsync(t => t.Id == id, ct);
		await hub.Clients.All.SendAsync("TourUpdated", tour, ct);

		return Ok(trans);
	}

	private static bool TryValidateTour(TourCreateDto dto, out string error)
	{
		error = string.Empty;

		if (string.IsNullOrWhiteSpace(dto.Name))
		{
			error = "Ten tour khong duoc de trong.";
			return false;
		}

		if (dto.EstimatedMinutes <= 0 || dto.EstimatedMinutes > 1440)
		{
			error = "EstimatedMinutes phai trong khoang 1..1440.";
			return false;
		}

		if (dto.Stops == null || dto.Stops.Count == 0)
		{
			error = "Tour phai co it nhat 1 diem dung.";
			return false;
		}

		if (dto.Stops.Any(s => s.PoiId <= 0))
		{
			error = "Moi diem dung phai co PoiId hop le.";
			return false;
		}

		if (dto.Stops.Any(s => s.StopOrder <= 0))
		{
			error = "StopOrder phai bat dau tu 1.";
			return false;
		}

		if (dto.Stops.Any(s => s.StayMinutes <= 0 || s.StayMinutes > 720))
		{
			error = "StayMinutes phai trong khoang 1..720.";
			return false;
		}

		// Kiem tra trung lap thu tu (StopOrder)
		var duplicateOrder = dto.Stops
			.GroupBy(s => s.StopOrder)
			.FirstOrDefault(g => g.Count() > 1);
		if (duplicateOrder != null)
		{
			error = $"Thu tu diem dung (StopOrder) bi trung: {duplicateOrder.Key}.";
			return false;
		}

		// Kiem tra trung lap dia diem (PoiId) - Vi (TourId, PoiId) la Primary Key
		var duplicatePoi = dto.Stops
			.GroupBy(s => s.PoiId)
			.FirstOrDefault(g => g.Count() > 1);
		if (duplicatePoi != null)
		{
			error = $"Dia diem (POI) bi trung: ID {duplicatePoi.Key}. Moi quan chi duoc xuat hien 1 lan trong tour.";
			return false;
		}

		return true;
	}

	private static string NormalizeLang(string? lang)
		=> string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLowerInvariant();
}
