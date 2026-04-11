using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanh.API.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AudioController(
	IWebHostEnvironment env,
	ILogger<AudioController> log,
	ITtsService tts) : ControllerBase
{
	private string AudioDir => Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "audio");

	[Authorize(Roles = "Admin,Owner"), HttpPost("upload")]
	[RequestSizeLimit(52_428_800)]
	public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string lang = "vi", CancellationToken ct = default)
	{
		if (file.Length == 0)
			return BadRequest(new { message = "File trong" });

		var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (ext is not (".mp3" or ".wav" or ".m4a"))
			return BadRequest(new { message = "Chi chap nhan mp3, wav, m4a" });

		Directory.CreateDirectory(AudioDir);
		var filename = $"{Guid.NewGuid():N}_{lang}{ext}";
		var path = Path.Combine(AudioDir, filename);
		await using (var stream = new FileStream(path, FileMode.Create))
			await file.CopyToAsync(stream, ct);

		var pathBase = Request.PathBase.Value?.TrimEnd('/') ?? "";
		var url = $"{Request.Scheme}://{Request.Host}{pathBase}/audio/{filename}";
		log.LogInformation("Audio uploaded {File}", filename);
		return Ok(new { url, filename });
	}

	[Authorize(Roles = "Admin,Owner"), HttpPost("generate-tts")]
	public async Task<IActionResult> GenerateTts([FromBody] TtsRequest req, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(req.Text))
			return BadRequest(new { message = "Text khong duoc de trong" });

		var text = req.Text.Trim();
		if (text.Length > 5000)
			return BadRequest(new { message = "Text qua dai, vui long gioi han duoi 5000 ky tu" });

		var lang = string.IsNullOrWhiteSpace(req.Lang) ? "vi" : req.Lang.Trim().ToLowerInvariant();
		var voice = string.IsNullOrWhiteSpace(req.Voice) ? "Chi" : req.Voice.Trim();

		var audioBytes = await tts.SynthesizeAsync(text, lang, voice);
		if (audioBytes.Length == 0)
		{
			return StatusCode(StatusCodes.Status503ServiceUnavailable, new
			{
				message = "Khong tao duoc audio tu TTS. Hay kiem tra cau hinh VoiceRss hoac AzureTTS."
			});
		}

		Directory.CreateDirectory(AudioDir);
		var filename = $"{Guid.NewGuid():N}_{lang}_tts.mp3";
		var path = Path.Combine(AudioDir, filename);
		await System.IO.File.WriteAllBytesAsync(path, audioBytes, ct);

		var pathBase = Request.PathBase.Value?.TrimEnd('/') ?? "";
		var url = $"{Request.Scheme}://{Request.Host}{pathBase}/audio/{filename}";
		log.LogInformation("Audio generated from TTS {File}", filename);
		return Ok(new { url, filename });
	}

	[Authorize(Roles = "Admin,Owner"), HttpPost("synthesize")]
	public async Task<IActionResult> Synthesize([FromBody] TtsRequest dto, CancellationToken ct = default)
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
					new { message = "TTS backend chua san sang. Kiem tra cau hinh VoiceRss hoac AzureTTS." });
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
