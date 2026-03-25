using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.API.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AiController(ApplicationDbContext db, IAiService ai) : ControllerBase
{
	[HttpPost("chat")]
	public async Task<IActionResult> Chat([FromBody] ChatRequest req)
	{
		// Build short context to keep prompt size manageable.
		var pois = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.ToListAsync();

		var tours = await db.Tours
			.Include(t => t.Stops)
				.ThenInclude(s => s.Poi)
			.AsNoTracking()
			.ToListAsync();

		var poiContext = string.Join("\n", pois.Select(p =>
		{
			var t = p.Translations.FirstOrDefault(x => x.LanguageCode == req.Language);
			return $"- [{p.Category}] {t?.Name ?? p.Name} (MapX:{p.MapX}%, MapY:{p.MapY}%): {t?.Description ?? p.Description}";
		}));

		var tourContext = string.Join("\n", tours.Select(t =>
			$"- Tour: {t.Name} ({t.EstimatedMinutes} phut) — " +
			string.Join(" -> ", t.Stops.OrderBy(s => s.StopOrder).Select(s => s.Poi?.Name))
		));

		var system = $"""
Ban la tro ly AI cua Pho Am Thuc Vinh Khanh - TP.HCM.
Nhiem vu:
1. Gioi thieu cac mon an, quan an tren pho
2. Dan duong khu vuc DUNG theo loai mon ho muon
3. Goi y Tour phu hop neu khach can lo trinh co san
4. Tra loi ve gio mo cua, gia ca, phuong tien di chuyen
LUON tra loi bang ngon ngu: {req.Language}
Danh sach quan: 
{poiContext}
{tourContext}
""";

		var reply = await ai.ChatAsync(system, req.Message, req.History);
		return Ok(new { reply });
	}
}

