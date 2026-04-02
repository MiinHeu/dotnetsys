using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanh.API.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TtsController(ITtsService tts) : ControllerBase
{
	[HttpPost("synthesize")]
	public async Task<IActionResult> Synthesize([FromBody] TtsRequest dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Text))
			return BadRequest(new { message = "Text khong duoc trong" });

		if (dto.Text.Length > 5000)
			return BadRequest(new { message = "Text qua dai, vui long duoi 5000 ky tu." });

		var lang = NormalizeLang(dto.Lang);
		var voice = string.IsNullOrWhiteSpace(dto.Voice)
			? DefaultVoice(lang)
			: dto.Voice.Trim();

		try
		{
			var audioBytes = await tts.SynthesizeAsync(dto.Text.Trim(), lang, voice);
			if (audioBytes.Length == 0)
			{
				return StatusCode(StatusCodes.Status503ServiceUnavailable,
					new { message = "TTS backend chua san sang. Kiem tra AzureTTS config." });
			}

			return File(audioBytes, "audio/mpeg", "speech.mp3");
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = $"TTS error: {ex.Message}" });
		}
	}

	private static string NormalizeLang(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang)) return "vi";
		var key = lang.Trim().ToLowerInvariant();
		if (key.Contains('-')) key = key[..2];
		return key;
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
}
