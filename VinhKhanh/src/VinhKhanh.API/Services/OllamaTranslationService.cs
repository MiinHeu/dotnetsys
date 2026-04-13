using System.Net.Http.Json;
using System.Text.Json;

namespace VinhKhanh.API.Services;

public class OllamaTranslationService(
	HttpClient httpClient,
	IConfiguration cfg,
	ILogger<OllamaTranslationService> logger) : ITranslationService
{
	private readonly string _baseUrl = cfg["Ollama:BaseUrl"] ?? "http://localhost:11434";
	private readonly string _model = cfg["Ollama:Model"] ?? "llama3.1:8b";

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

		fromLanguage = fromLanguage.ToLowerInvariant();
		toLanguage = toLanguage.ToLowerInvariant();

		// CƠ CHẾ DỊCH TRUNG GIAN (Pivot Translation):
		// Nếu yêu cầu dịch từ Việt -> (Trung/Nhật/Hàn...), ta sẽ dịch sang tiếng Anh trước để tăng độ chính xác.
		if (fromLanguage == "vi" && toLanguage != "en" && toLanguage != "vi")
		{
			logger.LogInformation("Using pivot translation: VI -> EN -> {ToLang}", toLanguage);
			
			// Bước 1: Việt -> Anh
			var englishText = await CallAiInternalAsync(text, "vi", "en", ct);
			if (!string.IsNullOrWhiteSpace(englishText))
			{
				// Bước 2: Anh -> Ngôn ngữ đích
				var finalResult = await CallAiInternalAsync(englishText, "en", toLanguage, ct);
				if (!string.IsNullOrWhiteSpace(finalResult))
				{
					return finalResult;
				}
			}
			
			logger.LogWarning("Pivot translation failed or returned empty. Falling back to direct translation.");
		}

		// Dịch trực tiếp (hoặc fallback nếu dịch trung gian thất bại)
		return await CallAiInternalAsync(text, fromLanguage, toLanguage, ct);
	}

	private async Task<string?> CallAiInternalAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
	{
		var langNames = GetLanguageNames(fromLanguage, toLanguage);
		
		var messages = new List<object>
		{
			new { role = "system", content = $"You are a professional translator into {langNames.to}. Output ONLY the translated text." },
			new { role = "user", content = $"Translate from {langNames.from} to {langNames.to} (Return ONLY the translation):\n{text}" }
		};

		var payload = new
		{
			model = _model,
			messages = messages,
			stream = false,
			options = new
			{
				temperature = 0.0,
				num_predict = 1024 // Tăng lên một chút để tránh bị cắt câu dài
			}
		};

		var url = $"{_baseUrl.TrimEnd('/')}/api/chat";

		try
		{
			using var response = await httpClient.PostAsJsonAsync(url, payload, ct);
			if (!response.IsSuccessStatusCode)
			{
				var errorBody = await response.Content.ReadAsStringAsync(ct);
				logger.LogWarning("Ollama call failed. Status={StatusCode}, Body={Body}", (int)response.StatusCode, errorBody);
				return null;
			}

			var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
			if (json.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
			{
				var raw = content.GetString()?.Trim() ?? string.Empty;

				// Hậu xử lý: Chỉ lấy chuỗi đã dịch
				string[] prefixesToRemove = { "Translation:", "Translated text:", "Result:", "Answer:" };
				foreach (var prefix in prefixesToRemove)
				{
					if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					{
						raw = raw[prefix.Length..].Trim();
					}
				}

				if (raw.StartsWith("```"))
				{
					var lines = raw.Split('\n');
					var filtered = lines.Where(l => !l.Trim().StartsWith("```")).ToArray();
					raw = string.Join('\n', filtered).Trim();
				}

				if (raw.Length >= 2 && ((raw.StartsWith('"') && raw.EndsWith('"')) || (raw.StartsWith('\'') && raw.EndsWith('\''))))
				{
					raw = raw[1..^1];
				}

				return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
			}

			return null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error while calling Ollama API.");
			return null;
		}
	}
}
