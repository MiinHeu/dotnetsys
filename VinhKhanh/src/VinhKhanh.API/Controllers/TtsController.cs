using Microsoft.AspNetCore.Mvc;
using VinhKhanh.API.Services;
using VinhKhanh.Shared.DTOs;

using Microsoft.AspNetCore.Authorization;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TtsController(ITtsService tts) : ControllerBase
{
	[HttpPost("synthesize")]
	public async Task<IActionResult> Synthesize([FromBody] TtsSynthesizeDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Text))
			return BadRequest("Text khong duoc trong");

		var voice = dto.Voice ?? dto.Lang switch
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

		try
		{
			var audioBytes = await tts.SynthesizeAsync(dto.Text, dto.Lang, voice);
			return File(audioBytes, "audio/mpeg", "speech.mp3");
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Azure TTS error: {ex.Message}");
		}
	}
}
