using System.Net.Http.Json;
using System.Text.Json;

namespace VinhKhanh.API.Services;

public class OllamaTranslationService(
	HttpClient httpClient,
	IConfiguration cfg,
	ILogger<OllamaTranslationService> logger) : ITranslationService
{
	private readonly string _baseUrl = cfg["Ollama:BaseUrl"] ?? "http://localhost:11434";
	private readonly string _model = cfg["Ollama:Model"] ?? "llama3.2:3b";

	private static (string from, string to) GetLanguageNames(string fromLang, string toLang)
	{
		var names = new Dictionary<string, string>
		{
			["vi"] = "Vietnamese",
			["en"] = "English",
			["zh"] = "Chinese",
			["zh-CN"] = "Chinese",
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

		// Enhanced system prompt for accurate, literal translation
		var langNames = GetLanguageNames(fromLanguage, toLanguage);
		var systemPrompt = $$"""
			You are a precise translator. Translate EXACTLY word-for-word. Do NOT paraphrase, explain, add comments, or use different wording.
			Keep the original meaning and tone 100%. Return ONLY the translated text, nothing else.
			Example: "xin chào" -> "hello" (en), "你好" (zh), "こんにちは" (ja).
			""";

		// Add few-shot examples for common phrases to improve accuracy
		var userPrompt = $$"""
			Translate from {{langNames.from}} to {{langNames.to}}.

			Examples:
			- "xin chào" -> "hello" (en), "你好" (zh), "こんにちは" (ja)
			- "cảm ơn" -> "thank you" (en), "谢谢" (zh), "ありがとう" (ja)
			- "tạm biệt" -> "goodbye" (en), "再见" (zh), "さようなら" (ja)

			Now translate:
			"{{text}}"
			""";

		var payload = new
		{
			model = _model,
			messages = new[]
			{
				new { role = "system", content = systemPrompt },
				new { role = "user", content = userPrompt }
			},
			stream = false,
			options = new
			{
				temperature = 0.0, // Zero for deterministic, consistent output
				top_p = 0.9,
				num_predict = 512
			}
		};

		var url = $"{_baseUrl.TrimEnd('/')}/api/chat";

		try
		{
			using var response = await httpClient.PostAsJsonAsync(url, payload, ct);
			if (!response.IsSuccessStatusCode)
			{
				var errorBody = await response.Content.ReadAsStringAsync(ct);
				logger.LogWarning("Ollama translation failed. Status={StatusCode}, Body={Body}", (int)response.StatusCode, errorBody);
				return null;
			}

			var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
			if (json.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
			{
				var raw = content.GetString()?.Trim() ?? string.Empty;

				// Post-process: extract only the translation
				// Remove common prefixes/suffixes that LLMs might add
				raw = raw
					.Replace("Translation:", "", StringComparison.OrdinalIgnoreCase)
					.Replace("Translated text:", "", StringComparison.OrdinalIgnoreCase)
					.Replace("Result:", "", StringComparison.OrdinalIgnoreCase)
					.Trim();

				// If there are multiple lines, take the first non-empty line
				var lines = raw.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
				if (lines.Length > 0)
				{
					raw = lines[0].Trim();
				}

				// Remove surrounding quotes if present
				if ((raw.StartsWith('"') && raw.EndsWith('"')) || (raw.StartsWith('\'') && raw.EndsWith('\'')))
				{
					raw = raw[1..^1];
				}

				return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
			}

			return null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error while translating using Ollama.");
			return null;
		}
	}
}
