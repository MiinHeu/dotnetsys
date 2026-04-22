using System.Security;
using System.Text;

namespace VinhKhanh.API.Services;

/// <summary>
/// Azure Speech TTS via REST API.
/// Returns empty payload when Azure credentials are not configured so client can fallback to local TTS.
/// </summary>
public class AzureTtsService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<AzureTtsService> logger) : ITtsService
{
	private const string DefaultOutputFormat = "audio-16khz-128kbitrate-mono-mp3";

	public async Task<byte[]> SynthesizeAsync(string text, string lang, string voice)
	{
		if (string.IsNullOrWhiteSpace(text))
			return Array.Empty<byte>();

		var key = cfg["AzureTTS:Key"];
		var region = cfg["AzureTTS:Region"];

		if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(region))
		{
			logger.LogWarning("Azure TTS config is missing. Falling back to client-side TTS.");
			return Array.Empty<byte>();
		}

		var resolvedLang = NormalizeLang(lang);
		var resolvedVoice = NormalizeVoice(voice, resolvedLang);
		var outputFormat = cfg["AzureTTS:OutputFormat"] ?? DefaultOutputFormat;

		var escapedText = SecurityElement.Escape(text.Trim()) ?? string.Empty;
		var escapedLang = SecurityElement.Escape(resolvedLang) ?? "vi-VN";
		var escapedVoice = SecurityElement.Escape(resolvedVoice) ?? DefaultVoiceFor(resolvedLang);

		var ssml = $$"""
		             <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{{escapedLang}}">
		               <voice name="{{escapedVoice}}">{{escapedText}}</voice>
		             </speak>
		             """;

		var endpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";

		try
		{
			using var http = httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
			req.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", key);
			req.Headers.TryAddWithoutValidation("X-Microsoft-OutputFormat", outputFormat);
			req.Headers.TryAddWithoutValidation("User-Agent", "VinhKhanh.API");
			req.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

			logger.LogInformation("Azure TTS Request: {Url}", endpoint);
			using var res = await http.SendAsync(req);
			if (!res.IsSuccessStatusCode)
			{
				var body = await res.Content.ReadAsStringAsync();
				var headers = string.Join("; ", res.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
				logger.LogError("Azure TTS Error! Status={StatusCode}, Body={Body}, URL={Url}, Headers={Headers}", (int)res.StatusCode, body, endpoint, headers);
				return Array.Empty<byte>();
			}

			return await res.Content.ReadAsByteArrayAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Azure TTS call failed unexpectedly.");
			return Array.Empty<byte>();
		}
	}

	private static string NormalizeLang(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang))
			return "vi-VN";

		var raw = lang.Trim();
		var key = raw.ToLowerInvariant();
		return key switch
		{
			"vi" => "vi-VN",
			"en" => "en-US",
			"zh" => "zh-CN",
			"ko" => "ko-KR",
			"ja" => "ja-JP",
			"km" => "km-KH",
			"th" => "th-TH",
			_ when key.Contains('-') => raw,
			_ => "vi-VN"
		};
	}

	private static string NormalizeVoice(string voice, string lang)
	{
		if (string.IsNullOrWhiteSpace(voice))
			return DefaultVoiceFor(lang);

		var v = voice.Trim().ToLowerInvariant();

		// Ánh xạ từ các tên cũ (VoiceRSS) sang Azure
		return v switch
		{
			"chi" => "vi-VN-HoaiMyNeural",
			"linda" => "en-US-JennyNeural",
			"luli" => "zh-CN-XiaoxiaoNeural",
			"nari" => "ko-KR-SunHiNeural",
			"hina" => "ja-JP-NanamiNeural",
			_ when v.Contains('-') => voice.Trim(), // Nếu đã có định dạng Azure chuẩn
			_ => DefaultVoiceFor(lang)
		};
	}

	private static string DefaultVoiceFor(string lang)
		=> lang switch
		{
			"vi-VN" => "vi-VN-HoaiMyNeural",
			"en-US" => "en-US-AriaNeural",
			"zh-CN" => "zh-CN-XiaoxiaoNeural",
			"ko-KR" => "ko-KR-SunHiNeural",
			"ja-JP" => "ja-JP-NanamiNeural",
			"km-KH" => "km-KH-SreymomNeural",
			"th-TH" => "th-TH-PremwadeeNeural",
			_ => "vi-VN-HoaiMyNeural"
		};
}
