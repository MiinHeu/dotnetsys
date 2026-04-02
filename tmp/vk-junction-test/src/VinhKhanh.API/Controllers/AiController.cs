using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AiController(
	ApplicationDbContext db,
	IAiService ai,
	ITtsService tts,
	ILogger<AiController> logger) : ControllerBase
{
	[HttpPost("chat")]
	public async Task<IActionResult> Chat([FromBody] ChatRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.Message))
			return BadRequest(new { message = "Message khong duoc de trong." });

		var lang = NormalizeLang(req.Language);
		var userMessage = req.Message.Trim();

		var pois = await db.Pois
			.Include(p => p.Translations)
			.AsNoTracking()
			.ToListAsync(ct);

		var tours = await db.Tours
			.Where(t => t.IsActive)
			.Include(t => t.Stops.OrderBy(s => s.StopOrder))
				.ThenInclude(s => s.Poi)
			.AsNoTracking()
			.ToListAsync(ct);

		var poiContext = string.Join("\n", pois.Select(p =>
		{
			var name = ResolvePoiName(p, lang);
			var desc = ResolvePoiDescription(p, lang);
			return $"- [{p.Category}] {name} (MapX:{p.MapX}%, MapY:{p.MapY}%): {desc}";
		}));

		var tourContext = string.Join("\n", tours.Select(t =>
			$"- Tour: {t.Name} ({t.EstimatedMinutes} phut) - " +
			string.Join(" -> ", t.Stops.OrderBy(s => s.StopOrder).Select(s => s.Poi?.Name ?? $"POI#{s.PoiId}"))
		));

		var system = $$"""
		               Ban la tro ly AI cua Pho Am Thuc Vinh Khanh (Quan 4, TP.HCM).
		               Nhiem vu:
		               1. Gioi thieu mon an, quan an va diem thu vi trong khu pho.
		               2. Ho tro tiep dan va huong dan cong dan den dung phong/chuc nang khi duoc yeu cau.
		               3. Goi y tour phu hop theo nhu cau.
		               4. Tra loi ngan gon, de hieu, lich su.
		               Luon tra loi bang ngon ngu: {{lang}}.
		               Danh sach POI:
		               {{poiContext}}
		               Danh sach tour:
		               {{tourContext}}
		               """;

		var safeHistory = req.History?
			.Where(h => !string.IsNullOrWhiteSpace(h.Content))
			.TakeLast(10)
			.Select(h => new MessageHistory(NormalizeRole(h.Role), h.Content.Trim()))
			.ToList();

		try
		{
			var reply = await ai.ChatAsync(system, userMessage, safeHistory);
			if (string.IsNullOrWhiteSpace(reply))
				reply = "Xin loi, toi chua co cau tra loi phu hop ngay luc nay.";

			return Ok(new { reply, language = lang });
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "AI chat endpoint failed.");
			return Ok(new
			{
				reply = "Xin loi, he thong AI dang tam thoi gian doan. Vui long thu lai sau.",
				language = lang
			});
		}
	}

	[HttpPost("tts")]
	public async Task<IActionResult> Synthesize([FromBody] TtsRequest req)
	{
		if (string.IsNullOrWhiteSpace(req.Text))
			return BadRequest(new { message = "Text khong duoc de trong." });

		if (req.Text.Length > 5000)
			return BadRequest(new { message = "Text qua dai, vui long gioi han duoi 5000 ky tu." });

		var lang = NormalizeLang(req.Lang);
		var voice = string.IsNullOrWhiteSpace(req.Voice)
			? DefaultVoice(lang)
			: req.Voice.Trim();

		var audioBytes = await tts.SynthesizeAsync(req.Text.Trim(), lang, voice);
		if (audioBytes.Length == 0)
			return StatusCode(StatusCodes.Status503ServiceUnavailable,
				new { message = "TTS server chua san sang. App co the fallback local TTS." });

		return File(audioBytes, "audio/mpeg", "speech.mp3");
	}

	private static string NormalizeLang(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang))
			return "vi";

		var key = lang.Trim().ToLowerInvariant();
		if (key.Contains('-'))
			key = key[..2];

		return key is "vi" or "en" or "zh" or "ko" or "ja" or "km" or "th"
			? key
			: "vi";
	}

	private static string NormalizeRole(string? role)
	{
		if (string.IsNullOrWhiteSpace(role))
			return "user";

		var key = role.Trim().ToLowerInvariant();
		return key is "system" or "assistant" or "user" ? key : "user";
	}

	private static string DefaultVoice(string lang)
		=> lang switch
		{
			"vi" => "vi-VN-HoaiMyNeural",
			"en" => "en-US-AriaNeural",
			"zh" => "zh-CN-XiaoxiaoNeural",
			"ko" => "ko-KR-SunHiNeural",
			"ja" => "ja-JP-NanamiNeural",
			"km" => "km-KH-SreymomNeural",
			"th" => "th-TH-PremwadeeNeural",
			_ => "vi-VN-HoaiMyNeural"
		};

	private static string ResolvePoiName(Poi p, string lang)
	{
		var translated = p.Translations.FirstOrDefault(t =>
			t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase));
		return string.IsNullOrWhiteSpace(translated?.Name) ? p.Name : translated.Name;
	}

	private static string ResolvePoiDescription(Poi p, string lang)
	{
		var translated = p.Translations.FirstOrDefault(t =>
			t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(translated?.Description))
			return translated.Description;

		translated = p.Translations.FirstOrDefault(t =>
			t.LanguageCode.Equals("en", StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(translated?.Description))
			return translated.Description;

		return p.Description;
	}
}
