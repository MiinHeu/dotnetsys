using Google.GenAI;
using System.Text.Json;

namespace VinhKhanh.API.Services;

/// <summary>
/// Google Gemini AI Translation Service (using Google.GenAI SDK).
/// Primary provider for high-quality, high-speed translations.
/// </summary>
public class GeminiTranslationService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<GeminiTranslationService> logger) : ITranslationService
{
	private readonly string? _apiKey = cfg["Gemini:ApiKey"];
	private readonly string _model = cfg["Gemini:Model"] ?? "gemini-1.5-flash";

	private static (string from, string to) GetLanguageNames(string fromLang, string toLang)
	{
		var names = new Dictionary<string, string>
		{
			["vi"] = "Vietnamese",
			["en"] = "English",
			["zh"] = "Chinese",
			["zh-cn"] = "Chinese",
			["ko"] = "Korean",
			["ja"] = "Japanese",
			["th"] = "Thai",
			["km"] = "Khmer",
			["fr"] = "French",
			["de"] = "German",
			["es"] = "Spanish",
			["ru"] = "Russian"
		};

		var from = names.TryGetValue(fromLang.ToLowerInvariant(), out var f) ? f : fromLang;
		var to = names.TryGetValue(toLang.ToLowerInvariant(), out var t) ? t : toLang;

		return (from, to);
	}

	public async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		if (string.IsNullOrWhiteSpace(_apiKey))
		{
			logger.LogWarning("Gemini API Key is missing.");
			return null;
		}

		try
		{
			var langNames = GetLanguageNames(fromLanguage, toLanguage);
			var prompt = $"Assign yourself as a professional translator. Translate precisely from {langNames.from} to {langNames.to}. " +
			             $"STRICT RULES: Output ONLY the translated text. Maintain the formal/historical tone. " +
			             $"Ensure names like 'Hai Bà Trưng' are translated correctly into {langNames.to} (e.g., 'The Trung Sisters' for English, '征侧和征贰' for Chinese).\n\n" +
			             $"Text to translate:\n{text}";

			var payload = new
			{
				contents = new[]
				{
					new { parts = new[] { new { text = prompt } } }
				}
			};

			var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
			
			using var http = httpClientFactory.CreateClient();
			using var response = await http.PostAsJsonAsync(url, payload, ct);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync(ct);
				logger.LogError("Gemini API Error: Status={Status}, Detail={Detail}", response.StatusCode, error);
				return null;
			}

			var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
			var translated = result.GetProperty("candidates")[0]
				.GetProperty("content")
				.GetProperty("parts")[0]
				.GetProperty("text")
				.GetString()?.Trim();

			return CleanResult(translated);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Gemini call failed.");
			return null;
		}
	}

	private static string? CleanResult(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw)) return null;
		
		var result = raw;
		string[] prefixes = { "Translation:", "Translated text:", "Result:" };
		foreach (var p in prefixes)
		{
			if (result.StartsWith(p, StringComparison.OrdinalIgnoreCase))
				result = result[p.Length..].Trim();
		}

		if (result.StartsWith("```"))
		{
			var lines = result.Split('\n');
			result = string.Join('\n', lines.Where(l => !l.Trim().StartsWith("```"))).Trim();
		}

		return result;
	}
}
