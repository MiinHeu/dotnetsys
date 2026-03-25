using Microsoft.CognitiveServices.Speech;

namespace VinhKhanh.API.Services;

public class AzureTtsService(IConfiguration cfg) : ITtsService
{
	public async Task<byte[]> SynthesizeAsync(string text, string lang, string voice)
	{
		var key = cfg["AzureTTS:Key"];
		var region = cfg["AzureTTS:Region"];
		
		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(region))
		{
			throw new InvalidOperationException("AzureTTS is not configured properly in appsettings.");
		}

		var sc = SpeechConfig.FromSubscription(key, region);
		sc.SpeechSynthesisVoiceName = voice;
		
		using var synth = new SpeechSynthesizer(sc, null);
		var result = await synth.SpeakTextAsync(text);
		return result.AudioData;
	}
}
