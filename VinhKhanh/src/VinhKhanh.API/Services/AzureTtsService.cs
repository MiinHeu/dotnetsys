namespace VinhKhanh.API.Services;

/// <summary>
/// Fallback TTS service khi chưa cấu hình Azure Cognitive Services.
/// App MAUI đã dùng TTS bản địa (TextToSpeech.Default) → hoạt động offline, miễn phí.
/// Server-side TTS chỉ cần khi muốn pre-generate audio files chất lượng cao.
/// </summary>
public class AzureTtsService(IConfiguration cfg) : ITtsService
{
	public Task<byte[]> SynthesizeAsync(string text, string lang, string voice)
	{
		var key = cfg["AzureTTS:Key"];
		var region = cfg["AzureTTS:Region"];

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(region))
		{
			throw new InvalidOperationException(
				"Azure TTS chưa được cấu hình. " +
				"App sẽ dùng giọng đọc TTS bản địa trên điện thoại (miễn phí, offline). " +
				"Để bật Azure TTS, thêm 'AzureTTS:Key' và 'AzureTTS:Region' vào appsettings.json.");
		}

		// Khi có Azure key, sẽ gọi Azure Cognitive Services
		// Hiện tại chưa cấu hình → throw ở trên
		throw new NotImplementedException("Azure TTS integration — cần cài Microsoft.CognitiveServices.Speech NuGet package.");
	}
}
