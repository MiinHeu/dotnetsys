using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AudioController(IWebHostEnvironment env, ILogger<AudioController> log) : ControllerBase
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
}
