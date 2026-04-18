using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using VinhKhanh.API.Auth;
using VinhKhanh.API.Hubs;
using VinhKhanh.Shared;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class PoiController(
	ApplicationDbContext db,
	IHubContext<VinhKhanhHub> hub,
	VinhKhanh.API.Services.ITranslationService translator,
	ILogger<PoiController> logger) : ControllerBase
{
	private readonly VinhKhanh.API.Services.ITranslationService _translator = translator;
	private readonly ILogger<PoiController> _logger = logger;
	private static string? NormalizeQr(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
			return null;

		return code.Trim().ToUpperInvariant();
	}

	private static string NormalizeLang(string? lang)
		=> string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLowerInvariant();

	private int? CurrentUserId
	{
		get
		{
			if (AuthClaims.TryGetUserId(HttpContext.User, out var id))
				return id;
			return null;
		}
	}

	private bool IsOwnerRole => AuthClaims.IsOwner(HttpContext.User);

	private bool CanAccessPoi(Poi poi)
	{
		if (AuthClaims.IsAdmin(HttpContext.User)) return true;
		if (!IsOwnerRole) return true; // public read
		var uid = CurrentUserId;
		return uid == null || poi.OwnerUserId == null || poi.OwnerUserId == uid;
	}

	[HttpGet]
	[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
	public async Task<IActionResult> GetAll([FromQuery] string lang = "vi", CancellationToken ct = default)
	{
		_ = NormalizeLang(lang);

		var query = db.Pois
			.Where(p => p.IsActive)
			.Include(p => p.Translations)
			.Include(p => p.Owner)
			.AsNoTracking();

		// Nếu là Owner (và không phải Admin), chỉ cho phép thấy POI của chính mình quản lý
		if (IsOwnerRole && !AuthClaims.IsAdmin(HttpContext.User))
		{
			var uid = CurrentUserId;
			if (uid.HasValue)
			{
				query = query.Where(p => p.OwnerUserId == uid.Value);
			}
		}

		try
		{
			var pois = await query
				.OrderByDescending(p => p.Priority)
				.ThenBy(p => p.Name)
				.ToListAsync(ct);

			return Ok(pois);
		}
		catch (OperationCanceledException)
		{
			// Xảy ra khi client ngắt kết nối đột ngột
			return NoContent();
		}
	}

	[HttpGet("{id:int}")]
	[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
	public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
	{
		var poi = await db.Pois
			.IgnoreQueryFilters()
			.Include(p => p.Translations)
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == id, ct);

		if (poi == null) return NotFound();
		if (!CanAccessPoi(poi)) return Forbid();

		return Ok(poi);
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

		if (poi == null) return NotFound();
		if (!CanAccessPoi(poi)) return Forbid();

		return Ok(poi);
	}

	[HttpGet("{id:int}/qrcode")]
	[AllowAnonymous]
	public async Task<IActionResult> GenerateQrCode(int id, CancellationToken ct = default)
	{
		var poi = await db.Pois.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null) return NotFound(new { message = "POI khong ton tai." });

		if (string.IsNullOrWhiteSpace(poi.QrCode))
			return BadRequest(new { message = "POI nay chua duoc gan ma QR." });

		try
		{
			using var qrGenerator = new QRCoder.QRCodeGenerator();
			var qrCodeData = qrGenerator.CreateQrCode(poi.QrCode, QRCoder.QRCodeGenerator.ECCLevel.Q);
			var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
			byte[] qrCodeImage = qrCode.GetGraphic(20);

			return File(qrCodeImage, "image/png");
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "Loi tao QR code", detail = ex.Message });
		}
	}

	[HttpPost("nearby")]
	public async Task<IActionResult> FindNearby([FromBody] LocationQueryDto loc, CancellationToken ct = default)
	{
		if (!IsValidCoordinate(loc.Lat, loc.Lon))
			return BadRequest(new { message = "Toa do khong hop le." });

		try
		{
			var pois = await db.Pois
				.Where(p => p.IsActive)
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
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	[HttpPost]
	[Authorize(Roles = "Admin,Owner")]
	public async Task<IActionResult> Create([FromBody] Poi poi, CancellationToken ct = default)
	{
		TrimPoiFields(poi);
		if (!TryValidatePoi(poi, out var error))
			return BadRequest(new { message = error });

		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		// Owner: tự động gán OwnerUserId
		if (IsOwnerRole)
		{
			poi.OwnerUserId = CurrentUserId;
		}

		// Check trùng vị trí (cùng lat/lon với POI đã có, bất kể owner nào)
		var existingAtLocation = await db.Pois.IgnoreQueryFilters()
			.AnyAsync(p => Math.Abs(p.Latitude - poi.Latitude) < 1e-9
			            && Math.Abs(p.Longitude - poi.Longitude) < 1e-9, ct);
		if (existingAtLocation)
		{
			return Conflict(new { message = "Vi tri nay da co POI khac." });
		}

		poi.QrCode = NormalizeQr(poi.QrCode);
		NormalizeTranslations(poi.Translations, poi.Description);
		poi.CreatedAt = DateTime.UtcNow;
		poi.UpdatedAt = DateTime.UtcNow;
		poi.IsActive = true;
		if (poi.ContentVersion < 1) poi.ContentVersion = 1;

		db.Pois.Add(poi);
		await db.SaveChangesAsync(ct);

		// Tự động sinh mã QR nếu để trống (dựa trên ID vừa tạo)
		if (string.IsNullOrEmpty(poi.QrCode))
		{
			poi.QrCode = $"VK-POI-{poi.Id:D3}";
			await db.SaveChangesAsync(ct);
		}

		await hub.Clients.All.SendAsync("PoiCreated", poi, ct);
		return CreatedAtAction(nameof(GetById), new { id = poi.Id }, poi);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin,Owner")]
	public async Task<IActionResult> Update(int id, [FromBody] Poi updated, CancellationToken ct = default)
	{
		TrimPoiFields(updated);
		if (!TryValidatePoi(updated, out var error))
			return BadRequest(new { message = error });

		var poi = await db.Pois
			.IgnoreQueryFilters()
			.Include(p => p.Translations)
			.FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		// Owner chỉ được sửa POI của mình
		if (IsOwnerRole && !AuthClaims.IsAdmin(HttpContext.User))
		{
			var uid = CurrentUserId;
			if (uid == null || poi.OwnerUserId != uid)
				return Forbid();
		}

		// Check trùng vị trí (trừ chính nó)
		if (Math.Abs(updated.Latitude - poi.Latitude) > 1e-9 || Math.Abs(updated.Longitude - poi.Longitude) > 1e-9)
		{
			var existingAtLocation = await db.Pois.IgnoreQueryFilters()
				.AnyAsync(p => p.Id != id
				            && Math.Abs(p.Latitude - updated.Latitude) < 1e-9
				            && Math.Abs(p.Longitude - updated.Longitude) < 1e-9, ct);
			if (existingAtLocation)
			{
				return Conflict(new { message = "Vi tri nay da co POI khac." });
			}
		}

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

		// Chỉ Admin mới có quyền chuyển nhượng POI cho người khác
		if (AuthClaims.IsAdmin(HttpContext.User))
		{
			contentChanged = contentChanged || poi.OwnerUserId != updated.OwnerUserId;
			poi.OwnerUserId = updated.OwnerUserId;
		}

		var translationsChanged = false;
		if (updated.Translations != null)
		{
			foreach (var upt in updated.Translations)
			{
				var existingT = poi.Translations.FirstOrDefault(t => t.LanguageCode == upt.LanguageCode);
				if (existingT == null || 
				    existingT.Name != upt.Name || 
				    existingT.Description != upt.Description || 
				    existingT.AudioUrl != upt.AudioUrl)
				{
					translationsChanged = true;
					break;
				}
			}
		}

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

		if (contentChanged || translationsChanged) poi.ContentVersion++;

		poi.UpdatedAt = DateTime.UtcNow;

		// Sync translations
		if (updated.Translations != null)
		{
			foreach (var upt in updated.Translations)
			{
				var existingT = poi.Translations.FirstOrDefault(t => t.LanguageCode == upt.LanguageCode);
				if (existingT != null)
				{
					existingT.Name = upt.Name;
					existingT.Description = upt.Description;
					existingT.AudioUrl = upt.AudioUrl;
					existingT.OriginalDescription = upt.OriginalDescription;
				}
				else
				{
					poi.Translations.Add(new PoiTranslation
					{
						LanguageCode = upt.LanguageCode,
						Name = upt.Name,
						Description = upt.Description,
						AudioUrl = upt.AudioUrl,
						OriginalDescription = upt.OriginalDescription
					});
				}
			}
		}

		NormalizeTranslations(poi.Translations, poi.Description);

		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("PoiUpdated", poi, ct);
		return Ok(poi);
	}

	[HttpPost("{id:int}/translation")]
	[Authorize(Roles = "Admin,Owner")]
	public async Task<IActionResult> AddTranslation(int id, [FromBody] PoiTranslationDto dto, CancellationToken ct = default)
	{
		var lang = NormalizeLang(dto.LanguageCode);
		if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Description))
			return BadRequest(new { message = "Name va Description ban dich khong duoc trong." });

		var poi = await db.Pois.FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		// Owner chỉ được thêm translation cho POI của mình
		if (IsOwnerRole && !AuthClaims.IsAdmin(HttpContext.User))
		{
			var uid = CurrentUserId;
			if (uid == null || poi.OwnerUserId != uid)
				return Forbid();
		}

		var existing = await db.PoiTranslations
			.FirstOrDefaultAsync(t => t.PoiId == id && t.LanguageCode == lang, ct);

		if (existing != null)
		{
			existing.Name = dto.Name.Trim();
			existing.Description = dto.Description.Trim();
			existing.AudioUrl = string.IsNullOrWhiteSpace(dto.AudioUrl) ? null : dto.AudioUrl.Trim();
			if (string.IsNullOrWhiteSpace(existing.OriginalDescription))
				existing.OriginalDescription = poi.Description;
		}
		else
		{
			db.PoiTranslations.Add(new PoiTranslation
			{
				PoiId = id,
				LanguageCode = lang,
				Name = dto.Name.Trim(),
				Description = dto.Description.Trim(),
				AudioUrl = string.IsNullOrWhiteSpace(dto.AudioUrl) ? null : dto.AudioUrl.Trim(),
				OriginalDescription = poi.Description
			});
		}

		poi.ContentVersion++;
		poi.UpdatedAt = DateTime.UtcNow;

		await db.SaveChangesAsync(ct);
		await hub.Clients.All.SendAsync("PoiUpdated", poi, ct);
		return Ok(new { message = "Translation saved", poiId = id, languageCode = lang });
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin,Owner")]
	public async Task<IActionResult> Deactivate(int id, CancellationToken ct = default)
	{
		var poi = await db.Pois.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
		if (poi == null)
			return NotFound();

		// Owner chỉ được vô hiệu hoá POI của mình
		if (IsOwnerRole && !AuthClaims.IsAdmin(HttpContext.User))
		{
			var uid = CurrentUserId;
			if (uid == null || poi.OwnerUserId != uid)
				return Forbid();
		}

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

	private static void NormalizeTranslations(IEnumerable<PoiTranslation>? translations, string baseDescription)
	{
		if (translations == null) return;
		foreach (var t in translations)
		{
			t.LanguageCode = NormalizeLang(t.LanguageCode);
			t.Name = t.Name?.Trim() ?? string.Empty;
			t.Description = t.Description?.Trim() ?? string.Empty;
			t.AudioUrl = string.IsNullOrWhiteSpace(t.AudioUrl) ? null : t.AudioUrl.Trim();
			// Required field used to detect stale translations when the base POI description changes.
			if (string.IsNullOrWhiteSpace(t.OriginalDescription))
				t.OriginalDescription = baseDescription;
		}
	}
	[HttpPost("bulk-translate")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> BulkTranslate([FromQuery] bool overwrite = false, CancellationToken ct = default)
	{
		var targetLangs = new[] { "en", "ja", "ko", "zh" };
		var pois = await db.Pois.Include(p => p.Translations).ToListAsync(ct);
		var count = 0;

		Console.WriteLine("\n=== BAT DAU TIEN TRINH DICH THUAT HANG LOAT ===");
		_logger.LogInformation("Starting bulk translation for {Count} POIs", pois.Count);

		foreach (var poi in pois)
		{
			Console.WriteLine($"> Dang xu ly: {poi.Name} (ID: {poi.Id})");
			
			foreach (var lang in targetLangs)
			{
				var existing = poi.Translations.FirstOrDefault(t => t.LanguageCode == lang);
				if (existing != null && !overwrite && !string.IsNullOrWhiteSpace(existing.Description))
				{
					continue;
				}

				Console.WriteLine($"  - Dang dich sang [{lang.ToUpper()}]...");
				
				try
				{
					var translatedDesc = await _translator.TranslateAsync(poi.Description, "vi", lang, ct);
					if (!string.IsNullOrWhiteSpace(translatedDesc))
					{
						// Giữ nguyên tên gốc, chỉ dịch mô tả
						var translatedName = poi.Name;

						if (existing != null)
						{
							existing.Name = translatedName;
							existing.Description = translatedDesc;
							existing.OriginalDescription = poi.Description;
						}
						else
						{
							poi.Translations.Add(new PoiTranslation
							{
								LanguageCode = lang,
								Name = translatedName,
								Description = translatedDesc,
								OriginalDescription = poi.Description
							});
						}

						Console.WriteLine($"    [OK] Xong: {translatedName}");
						count++;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"    [LOI] {lang}: {ex.Message}");
				}
			}

			if (count > 0)
			{
				poi.ContentVersion++;
				poi.UpdatedAt = DateTime.UtcNow;
				await db.SaveChangesAsync(ct);
			}
		}

		Console.WriteLine($"\n=== DA HOAN TAT: {count} ban dich moi ===");
		return Ok(new { message = "Bulk translation finished", totalNewTranslations = count });
	}
}
