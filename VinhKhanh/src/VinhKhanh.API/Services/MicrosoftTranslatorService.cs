using System.Text;
using System.Text.Json;

namespace VinhKhanh.API.Services;

public class MicrosoftTranslatorService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<MicrosoftTranslatorService> logger) : ITranslationService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var key = cfg["Translator:Key"];
		var endpoint = cfg["Translator:Endpoint"] ?? "https://api.cognitive.microsofttranslator.com";
		var region = cfg["Translator:Region"];

		if (string.IsNullOrWhiteSpace(key))
		{
			logger.LogWarning("Microsoft Translator key is missing.");
			return null;
		}

		try
		{
			var builder = new UriBuilder($"{endpoint.TrimEnd('/')}/translate");
			builder.Query =
				$"api-version=3.0&from={Uri.EscapeDataString(NormalizeLang(fromLanguage))}&to={Uri.EscapeDataString(NormalizeLang(toLanguage))}";

			var body = JsonSerializer.Serialize(new[] { new TranslationInput(text.Trim()) }, JsonOptions);

			using var http = httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Post, builder.Uri);
			req.Content = new StringContent(body, Encoding.UTF8, "application/json");
			req.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", key);
			if (!string.IsNullOrWhiteSpace(region))
				req.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", region);
			req.Headers.TryAddWithoutValidation("X-ClientTraceId", Guid.NewGuid().ToString());

			using var res = await http.SendAsync(req, ct);
			var responseBody = await res.Content.ReadAsStringAsync(ct);
			if (!res.IsSuccessStatusCode)
			{
				logger.LogWarning("Microsoft Translator failed. Status={StatusCode}, Body={Body}", (int)res.StatusCode, responseBody);
				return null;
			}

			var payload = JsonSerializer.Deserialize<List<TranslationResponseItem>>(responseBody, JsonOptions);
			var translated = payload?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text?.Trim();
			if (string.IsNullOrWhiteSpace(translated))
			{
				logger.LogWarning("Microsoft Translator returned empty translation payload.");
				return null;
			}

			return translated;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Microsoft Translator call failed unexpectedly.");
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

	private sealed record TranslationInput(string Text);

	private sealed record TranslationResponseItem(List<TranslationTextItem>? Translations);

	private sealed record TranslationTextItem(string? Text, string? To);
}
