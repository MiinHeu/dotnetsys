using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;
using Microsoft.Maui.Media;

namespace VinhKhanh.App.Services;

public sealed class NarrationService(IAudioManager audioManager, ILogger<NarrationService> logger) : INarrationService
{
	private IAudioPlayer? _player;
	private MemoryStream? _playbackStream;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private readonly Queue<(Poi poi, string language)> _queue = [];
	private readonly HashSet<string> _queuedKeys = [];
	private readonly Dictionary<string, DateTime> _recentlyPlayed = [];
	private bool _isProcessing;
	private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(25);

	public bool IsPlaying => _player?.IsPlaying ?? false;

	public Task StopAsync()
	{
		if (_player == null) return Task.CompletedTask;
		try
		{
			_player.Stop();
			_player.Dispose();
		}
		catch { /* ignore */ }
		finally
		{
			_player = null;
			_playbackStream?.Dispose();
			_playbackStream = null;
		}
		return Task.CompletedTask;
	}

	public Task EnqueueAsync(Poi poi, string language)
	{
		var key = BuildKey(poi.Id, language);
		if (_queuedKeys.Contains(key)) return Task.CompletedTask;
		if (_recentlyPlayed.TryGetValue(key, out var playedAt) &&
		    DateTime.UtcNow - playedAt < DuplicateWindow) return Task.CompletedTask;

		_queue.Enqueue((poi, language));
		_queuedKeys.Add(key);
		if (_isProcessing) return Task.CompletedTask;
		_ = ProcessQueueAsync();
		return Task.CompletedTask;
	}

	public Task StopCurrentAsync() => StopAsync();

	private async Task ProcessQueueAsync()
	{
		_isProcessing = true;
		try
		{
			while (_queue.Count > 0)
			{
				var (poi, language) = _queue.Dequeue();
				_queuedKeys.Remove(BuildKey(poi.Id, language));

				var poiSnapshot = new PoiSnapshot
				{
					Id = poi.Id,
					Name = poi.Name,
					Description = poi.Description,
					Latitude = poi.Latitude,
					Longitude = poi.Longitude,
					MapX = poi.MapX,
					MapY = poi.MapY,
					TriggerRadiusMeters = poi.TriggerRadiusMeters,
					CooldownSeconds = poi.CooldownSeconds,
					Priority = poi.Priority,
					ImageUrl = poi.ImageUrl,
					AudioViUrl = poi.AudioViUrl,
					Translations = poi.Translations?.Select(t => new PoiTranslationSnapshot
					{
						Id = t.Id,
						PoiId = t.PoiId,
						LanguageCode = t.LanguageCode,
						Name = t.Name,
						Description = t.Description,
						AudioUrl = t.AudioUrl,
						OriginalDescription = t.OriginalDescription
					}).ToList()
				};

				var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
				await PlayPoiAsync(poiSnapshot, language, apiRoot);
				_recentlyPlayed[BuildKey(poi.Id, language)] = DateTime.UtcNow;
			}
		}
		finally
		{
			_isProcessing = false;
		}
	}

	/// <summary>Phat thuyet minh; tra ve thoi luong nghe uoc tinh (giay) cho analytics.</summary>
	public async Task<int> PlayPoiAsync(PoiSnapshot poi, string lang, string apiRootTrimmed, CancellationToken ct = default)
	{
		await _gate.WaitAsync(ct);
		var sw = Stopwatch.StartNew();
		try
		{
			await StopAsync();

			var audioUrl = poi.ResolveAudioUrl(lang);
			var originalText = poi.Description; // Vietnamese original

			logger.LogInformation("PlayPoiAsync: POI={PoiId}, Lang={Lang}, AudioUrl={AudioUrl}, OriginalText={Text}",
				poi.Id, lang, audioUrl ?? "(null)", originalText?.Substring(0, Math.Min(50, originalText?.Length ?? 0)) ?? "(null)");

			// Try playing audio file first (from web-generated TTS)
			if (!string.IsNullOrWhiteSpace(audioUrl))
			{
				var abs = audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
					? FixLocalhostForAndroid(audioUrl)
					: $"{FixLocalhostForAndroid(apiRootTrimmed.TrimEnd('/'))}/{audioUrl.TrimStart('/')}";

				try
				{
					logger.LogInformation("Attempting to play audio from URL: {Url}", abs);
					using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
					var bytes = await http.GetByteArrayAsync(abs, ct);
					logger.LogInformation("Downloaded {Bytes} bytes", bytes.Length);
					var ext = Path.GetExtension(new Uri(abs).AbsolutePath);
					if (string.IsNullOrEmpty(ext)) ext = ".mp3";
					_playbackStream = new MemoryStream(bytes);
					_player = audioManager.CreatePlayer(_playbackStream);
					logger.LogInformation("Created audio player, starting playback...");
					_player.Play();
					int waitCount = 0;
					while (_player.IsPlaying && !ct.IsCancellationRequested)
					{
						await Task.Delay(150, ct);
						waitCount++;
						if (waitCount % 10 == 0)
							logger.LogDebug("Still playing... ({Count} iterations)", waitCount);
					}
					logger.LogInformation("Audio playback completed");
					return ElapsedListenSeconds(sw);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to play audio from URL, falling back to in-app translation + TTS");
					// Fallback to in-app translation + TTS
				}
			}

			// If no audio file available for this language, try in-app translation + TTS
			string? textToSpeak = null;

			if (lang == "vi")
			{
				// Vietnamese: use original text
				textToSpeak = originalText;
			}
			else if (!string.IsNullOrWhiteSpace(originalText))
			{
				// Need to translate from Vietnamese to target language
				logger.LogInformation("Translating from 'vi' to '{Lang}' via API...", lang);
				var translated = await TranslateTextAsync(originalText, "vi", lang, apiRootTrimmed, ct);
				if (!string.IsNullOrWhiteSpace(translated))
				{
					textToSpeak = translated;
					logger.LogInformation("Translation successful: {Text}", translated);
				}
				else
				{
					logger.LogWarning("Translation failed for POI {PoiId} to {Lang}", poi.Id, lang);
					// Optionally fallback to Vietnamese TTS
					if (audioUrl == null) // Only if we don't have audio to fallback
					{
						textToSpeak = originalText;
						logger.LogInformation("Falling back to Vietnamese TTS");
					}
				}
			}

			// Use device TTS for the text (either original or translated)
			if (!string.IsNullOrWhiteSpace(textToSpeak))
			{
				try
				{
					logger.LogInformation("Using device TTS for language: {Lang}", lang);
					var locale = await PickLocaleAsync(lang, ct);
					logger.LogInformation("TTS locale selected: {Locale}", locale?.Language ?? "default");

					if (locale != null)
					{
						var options = new SpeechOptions
						{
							Locale = locale,
							Volume = 1f,
							Pitch = 1f,
							Rate = 0.92f
						};
						await TextToSpeech.Default.SpeakAsync(textToSpeak, options);
					}
					else
					{
						await TextToSpeech.Default.SpeakAsync(textToSpeak);
					}

					logger.LogInformation("TTS completed successfully");
					return ElapsedListenSeconds(sw);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "TTS failed for text: {Text}", textToSpeak);
					return ElapsedListenSeconds(sw);
				}
			}

			logger.LogWarning("No audio URL and no text available for POI {PoiId}", poi.Id);
			return ElapsedListenSeconds(sw);
		}
		finally
		{
			_gate.Release();
		}
	}

	private static int ElapsedListenSeconds(Stopwatch sw)
	{
		sw.Stop();
		var s = (int)Math.Round(sw.Elapsed.TotalSeconds);
		return s < 0 ? 0 : s;
	}

	private static async Task<Locale?> PickLocaleAsync(string lang, CancellationToken ct)
	{
		try
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync();
			var match = locales.FirstOrDefault(l => l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
				?? locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
			return match;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to get TTS locales: {ex}");
			return null;
		}
	}

	private static string BuildKey(int poiId, string lang)
		=> $"{poiId}:{lang.Trim().ToLowerInvariant()}";

	private static string FixLocalhostForAndroid(string url)
	{
		// On Android emulator, localhost/127.0.0.1 refers to the emulator itself
		// Use 10.0.2.2 to access host PC's localhost
		if (url.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
		    url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
		{
			var fixedUrl = url.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase)
			                  .Replace("127.0.0.1", "10.0.2.2", StringComparison.OrdinalIgnoreCase);
			Debug.WriteLine($"[NarrationService] Fixed localhost URL: {url} -> {fixedUrl}");
			return fixedUrl;
		}
		return url;
	}

	/// <summary>
	/// Gọi API dịch từ server để dịch text từ ngôn ngữ nguồn sang ngôn ngữ đích.
	/// </summary>
	private static async Task<string?> TranslateTextAsync(string text, string fromLang, string toLang, string apiRoot, CancellationToken ct)
	{
		try
		{
			var url = $"{FixLocalhostForAndroid(apiRoot)}/api/translation/text";
			var payload = new { text, from = fromLang, to = toLang };

			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
			var response = await http.PostAsJsonAsync(url, payload, ct);

			if (response.IsSuccessStatusCode)
			{
				var result = await response.Content.ReadFromJsonAsync<TranslationResponse>(ct);
				return result?.translatedText?.Trim();
			}

			var errorBody = await response.Content.ReadAsStringAsync(ct);
			Debug.WriteLine($"[NarrationService] Translation API failed: {(int)response.StatusCode} - {errorBody}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[NarrationService] Translation error: {ex}");
		}

		return null;
	}

	private sealed record TranslationResponse(string? translatedText);
}
