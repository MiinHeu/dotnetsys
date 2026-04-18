namespace VinhKhanh.API.Services;

/// <summary>
/// Translation strategy with fallback order:
/// Ollama -> LibreTranslate -> Microsoft Translator.
/// This prevents hard dependency on a single provider.
/// </summary>
public class ResilientTranslationService(
	GeminiTranslationService gemini,
	OllamaTranslationService ollama,
	LibreTranslateService libre,
	MicrosoftTranslatorService microsoft,
	IConfiguration cfg,
	ILogger<ResilientTranslationService> logger) : ITranslationService
{
	public async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		var providers = BuildProviderChain();
		foreach (var provider in providers)
		{
			try
			{
				var translated = await provider(text, fromLanguage, toLanguage, ct);
				if (!string.IsNullOrWhiteSpace(translated))
					return translated;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Translation provider failed. Trying fallback.");
			}
		}

		return null;
	}

	private IEnumerable<Func<string, string, string, CancellationToken, Task<string?>>> BuildProviderChain()
	{
		var hasGemini = !string.IsNullOrWhiteSpace(cfg["Gemini:ApiKey"]);
		var hasOllama = !string.IsNullOrWhiteSpace(cfg["Ollama:BaseUrl"]);
		var hasLibre = !string.IsNullOrWhiteSpace(cfg["LibreTranslate:BaseUrl"]);
		var hasMicrosoft = !string.IsNullOrWhiteSpace(cfg["Translator:Key"]);

		// Prefer high-quality cloud/local options first.
		if (hasGemini) yield return gemini.TranslateAsync;
		if (hasOllama) yield return ollama.TranslateAsync;
		if (hasLibre) yield return libre.TranslateAsync;
		if (hasMicrosoft) yield return microsoft.TranslateAsync;

		// Last safety net: try all providers even if config flags are missing.
		yield return gemini.TranslateAsync;
		yield return ollama.TranslateAsync;
		yield return libre.TranslateAsync;
		yield return microsoft.TranslateAsync;
	}
}
