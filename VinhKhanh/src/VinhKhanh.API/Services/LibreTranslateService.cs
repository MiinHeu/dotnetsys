using System.Text;
using System.Text.Json;

namespace VinhKhanh.API.Services;

public class LibreTranslateService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<LibreTranslateService> logger) : ITranslationService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var baseUrl = cfg["LibreTranslate:BaseUrl"]?.TrimEnd('/');
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			logger.LogWarning("LibreTranslate BaseUrl is missing.");
			return null;
		}

		var apiKey = cfg["LibreTranslate:ApiKey"];

		try
		{
			var endpoint = $"{baseUrl}/translate";
			var body = JsonSerializer.Serialize(new
			{
				q = text.Trim(),
				source = NormalizeLang(fromLanguage),
				target = NormalizeLang(toLanguage),
				format = "text",
				api_key = apiKey ?? ""
			}, JsonOptions);

			using var http = httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
			req.Content = new StringContent(body, Encoding.UTF8, "application/json");

			using var res = await http.SendAsync(req, ct);
			var responseBody = await res.Content.ReadAsStringAsync(ct);

			if (!res.IsSuccessStatusCode)
			{
				logger.LogWarning("LibreTranslate failed. Status={StatusCode}, Body={Body}", (int)res.StatusCode, responseBody);
				return null;
			}

			var payload = JsonSerializer.Deserialize<LibreTranslateResponse>(responseBody, JsonOptions);
			var translated = payload?.TranslatedText?.Trim();

			if (string.IsNullOrWhiteSpace(translated))
			{
				logger.LogWarning("LibreTranslate returned empty translation.");
				return null;
			}

			return System.Net.WebUtility.HtmlDecode(translated);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "LibreTranslate call failed unexpectedly.");
			return null;
		}
	}

	private static string NormalizeLang(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang))
			return "vi";

		var key = lang.Trim().ToLowerInvariant();
		if (key.Contains('-'))
			key = key[..2];

		return key;
	}

	private sealed record LibreTranslateResponse(string? TranslatedText, string? Error);
}
