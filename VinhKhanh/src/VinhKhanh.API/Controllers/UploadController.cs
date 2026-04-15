using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class UploadController(IWebHostEnvironment env, ILogger<UploadController> log) : ControllerBase
{
	private string ImageDir => Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "images");

	[Authorize(Roles = "Admin,Owner"), HttpPost("image")]
	[RequestSizeLimit(10_485_760)] // Giới hạn 10MB cho mỗi ảnh
	public async Task<IActionResult> UploadImage([FromForm] IFormFile file, CancellationToken ct = default)
	{
		if (file == null || file.Length == 0)
			return BadRequest(new { message = "File trống" });

		var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
		if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
			return BadRequest(new { message = "Chỉ chấp nhận các định dạng: jpg, jpeg, png, webp" });

		Directory.CreateDirectory(ImageDir);
		var filename = $"{Guid.NewGuid():N}{ext}";
		var path = Path.Combine(ImageDir, filename);

		await using (var stream = new FileStream(path, FileMode.Create))
			await file.CopyToAsync(stream, ct);

		var pathBase = Request.PathBase.Value?.TrimEnd('/') ?? "";
		var url = $"{Request.Scheme}://{Request.Host}{pathBase}/images/{filename}";
		
		log.LogInformation("Image uploaded successfully: {File}", filename);
		return Ok(new { url, filename });
	}
}
