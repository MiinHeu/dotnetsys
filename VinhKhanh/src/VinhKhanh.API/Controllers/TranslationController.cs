using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanh.API.Services;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class TranslationController(ITranslationService translationService) : ControllerBase
{
	[HttpPost("text")]
	public async Task<IActionResult> Translate([FromBody] TranslateTextRequest request, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(request.Text))
			return BadRequest(new { message = "Text khong duoc de trong." });

		if (string.IsNullOrWhiteSpace(request.To))
			return BadRequest(new { message = "Ngon ngu dich khong duoc de trong." });

		var translated = await translationService.TranslateAsync(request.Text.Trim(), request.From, request.To, ct);
		if (translated == null)
		{
			return StatusCode(StatusCodes.Status503ServiceUnavailable, new
			{
				message = "Dich vu Microsoft Translator chua san sang. Hay kiem tra cau hinh Translator."
			});
		}

		return Ok(new
		{
			translatedText = translated,
			from = string.IsNullOrWhiteSpace(request.From) ? "vi" : request.From.Trim().ToLowerInvariant(),
			to = request.To.Trim().ToLowerInvariant()
		});
	}
}

public sealed class TranslateTextRequest
{
	public string Text { get; set; } = string.Empty;
	public string From { get; set; } = "vi";
	public string To { get; set; } = "en";
}
