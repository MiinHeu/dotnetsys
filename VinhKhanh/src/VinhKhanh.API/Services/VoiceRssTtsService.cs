using System.Net.Http.Headers;

namespace VinhKhanh.API.Services;

/// <summary>
/// Voice RSS TTS via HTTP API.
/// Returns empty payload when Voice RSS credentials are not configured or the API rejects the request.
/// Docs: https://www.voicerss.org/api/
/// </summary>
public class VoiceRssTtsService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<VoiceRssTtsService> logger) : ITtsService
{
	private const string Endpoint = "https://api.voicerss.org/";

	public async Task<byte[]> SynthesizeAsync(string text, string lang, string voice)
	{
		if (string.IsNullOrWhiteSpace(text))
			return Array.Empty<byte>();

		var key = cfg["VoiceRss:ApiKey"];
		if (string.IsNullOrWhiteSpace(key))
		{
			logger.LogWarning("Voice RSS API key is missing. Falling back to next TTS option.");
			return Array.Empty<byte>();
		}

		var resolvedLang = NormalizeLang(lang);
		var resolvedVoice = NormalizeVoice(resolvedLang, voice);
		var codec = cfg["VoiceRss:Codec"] ?? "MP3";
		var format = cfg["VoiceRss:Format"] ?? "44khz_16bit_stereo";
		var rate = cfg["VoiceRss:Rate"] ?? "0";

		try
		{
			using var http = httpClientFactory.CreateClient();
			using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
			{
				Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["key"] = key,
					["src"] = text.Trim(),
					["hl"] = resolvedLang,
					["c"] = codec,
					["f"] = format,
					["r"] = rate
				})
			};

			if (!string.IsNullOrWhiteSpace(resolvedVoice))
				req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["key"] = key,
					["src"] = text.Trim(),
					["hl"] = resolvedLang,
					["v"] = resolvedVoice,
					["c"] = codec,
					["f"] = format,
					["r"] = rate
				});

			using var res = await http.SendAsync(req);
			if (!res.IsSuccessStatusCode)
			{
				var body = await res.Content.ReadAsStringAsync();
				logger.LogWarning("Voice RSS failed. Status={StatusCode}, Body={Body}", (int)res.StatusCode, body);
				return Array.Empty<byte>();
			}

			var mediaType = res.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
			if (mediaType is "text/plain" or "text/html" or "application/json")
			{
				var body = await res.Content.ReadAsStringAsync();
				logger.LogWarning("Voice RSS returned non-audio payload: {Body}", body);
				return Array.Empty<byte>();
			}

			var audio = await res.Content.ReadAsByteArrayAsync();
			if (audio.Length == 0)
			{
				logger.LogWarning("Voice RSS returned empty audio payload.");
				return Array.Empty<byte>();
			}

			return audio;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Voice RSS call failed unexpectedly.");
			return Array.Empty<byte>();
		}
	}

	private static string NormalizeLang(string? lang)
	{
		if (string.IsNullOrWhiteSpace(lang))
			return "vi-vn";

		var key = lang.Trim().ToLowerInvariant();
		return key switch
		{
			"vi" => "vi-vn",
			"en" => "en-us",
			"zh" => "zh-cn",
			"ko" => "ko-kr",
			"ja" => "ja-jp",
			"th" => "th-th",
			"km" => "en-us",
			_ => key
		};
	}

	private static string NormalizeVoice(string lang, string? voice)
	{
		if (!string.IsNullOrWhiteSpace(voice) && !voice.Contains('-'))
			return voice.Trim();

		return lang switch
		{
			"vi-vn" => "Chi",
			"en-us" => "Linda",
			"zh-cn" => "Luli",
			"ko-kr" => "Nari",
			"ja-jp" => "Hina",
			"th-th" => "Ukrit",
			_ => string.Empty
		};
	}
}
