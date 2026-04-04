namespace VinhKhanh.API.Services;

public interface ITranslationService
{
	Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct = default);
}
