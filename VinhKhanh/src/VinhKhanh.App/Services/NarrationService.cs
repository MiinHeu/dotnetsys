using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;
using Microsoft.Maui.Media;
using CommunityToolkit.Mvvm.Messaging;

namespace VinhKhanh.App.Services;

public sealed class NarrationService(
	IAudioManager audioManager, 
	ILogger<NarrationService> logger,
	AudioCacheService audioCache) : INarrationService
{
	private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
	private static readonly Dictionary<string, Locale> _localeCache = [];
	private IAudioPlayer? _player;
	private Stream? _playbackStream;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private readonly List<(Poi poi, string language, string triggerType)> _queue = [];
	private readonly HashSet<string> _queuedKeys = [];
	private readonly Dictionary<string, DateTime> _recentlyPlayed = [];
	private CancellationTokenSource? _playCts;
	private int _currentPriority;
	private bool _isProcessing;
	private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(25);
	private static readonly int MinListenSeconds = 5;

	/// <summary>Ngắt thuyết minh hiện tại nếu POI mới có priority cao hơn hoặc đang yêu cầu dừng.</summary>
	public void InterruptIfHigherPriority(int newPriority)
	{
		if (_currentPriority > 0 && newPriority > _currentPriority)
		{
			logger.LogInformation("Interrupting narration: new priority {New} > current {Current}", newPriority, _currentPriority);
			_playCts?.Cancel();
			_ = StopAsync();
		}
	}

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

	public Task EnqueueAsync(Poi poi, string language, string triggerType = "GPS")
	{
		var key = BuildKey(poi.Id, language);
		if (_queuedKeys.Contains(key)) return Task.CompletedTask;
		if (_recentlyPlayed.TryGetValue(key, out var playedAt) &&
		    DateTime.UtcNow - playedAt < DuplicateWindow) return Task.CompletedTask;

		_queue.Add((poi, language, triggerType));
		// Keep higher priority items processed first while preserving FIFO within same priority.
		_queue.Sort((a, b) => b.poi.Priority.CompareTo(a.poi.Priority));
		_queuedKeys.Add(key);
		InterruptIfHigherPriority(poi.Priority);
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
				var (poi, language, triggerType) = _queue[0];
				_queue.RemoveAt(0);
				_queuedKeys.Remove(BuildKey(poi.Id, language));
				_currentPriority = poi.Priority;
				_playCts?.Cancel();
				_playCts?.Dispose();
				_playCts = new CancellationTokenSource();

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
				
				// Thông báo bắt đầu phát để UI cập nhật và chuẩn bị ghi log
				WeakReferenceMessenger.Default.Send(new NarrationStartedMessage(poiSnapshot, language, triggerType));

				var duration = await PlayPoiAsync(poiSnapshot, language, apiRoot, _playCts.Token);
				_recentlyPlayed[BuildKey(poi.Id, language)] = DateTime.UtcNow;

				// Thông báo kết thúc phát kèm theo thời lượng thực tế và context
				WeakReferenceMessenger.Default.Send(new NarrationEndedMessage(poi.Id, duration, triggerType, language));

				if (_queue.Count > 0)
				{
					logger.LogInformation("Gap between queued narrations: 3 seconds");
					await Task.Delay(3000, _playCts.Token);
				}
			}
		}
		finally
		{
			_playCts?.Dispose();
			_playCts = null;
			_currentPriority = 0;
			_isProcessing = false;
		}
	}

	public async Task PreFetchAsync(PoiSnapshot poi, string language)
	{
		var audioUrl = poi.ResolveAudioUrl(language);
		if (string.IsNullOrWhiteSpace(audioUrl)) return;

		var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
		var abs = audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? FixLocalhostForAndroid(audioUrl)
			: $"{FixLocalhostForAndroid(apiRoot.TrimEnd('/'))}/{audioUrl.TrimStart('/')}";

		await audioCache.PreFetchAsync(abs);
	}

	public async Task PreFetchAllAsync(IEnumerable<PoiSnapshot> pois, string language)
	{
		if (pois == null) return;
		
		logger.LogInformation("Starting bulk pre-fetch for language: {Lang}", language);
		var tasks = pois.Select(p => PreFetchAsync(p, language)).ToList();
		await Task.WhenAll(tasks);
		logger.LogInformation("Completed bulk pre-fetch for language: {Lang}", language);
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

			logger.LogInformation("PlayPoiAsync: POI={PoiId}, Lang={Lang}, AudioUrl={AudioUrl}",
				poi.Id, lang, audioUrl ?? "(null)");

			bool audioPlayed = false;

			// Try playing audio file first (from web-generated TTS)
			if (!string.IsNullOrWhiteSpace(audioUrl))
			{
				string abs;
				if (audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					// Nếu DB lưu cứng URL là localhost hoặc 10.0.2.2, đè lại bằng API Root thực tế của điện thoại
					if (audioUrl.Contains("localhost") || audioUrl.Contains("127.0.0.1") || audioUrl.Contains("10.0.2.2"))
					{
						var uri = new Uri(audioUrl);
						abs = $"{apiRootTrimmed.TrimEnd('/')}{uri.PathAndQuery}";
					}
					else
					{
						abs = audioUrl;
					}
				}
				else
				{
					abs = $"{apiRootTrimmed.TrimEnd('/')}/{audioUrl.TrimStart('/')}";
				}

				try
				{
					logger.LogInformation("Attempting to play audio from URL: {Url}", abs);
					
					// SỬ DỤNG CACHE ĐỂ PHÁT NGAY LẬP TỨC
					var localPath = await audioCache.GetAudioPathAsync(abs, highPriority: true, ct);
					if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
					{
						logger.LogInformation("Playing from local file: {Path}", localPath);
						var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
						_playbackStream = fs;
						_player = audioManager.CreatePlayer(_playbackStream);
						_player.Play();
						audioPlayed = true;
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to play audio from URL, falling back to in-app translation + TTS");
				}
			}

			if (!audioPlayed)
			{
				// If no audio file available, try in-app translation + TTS
				string? textToSpeak = null;

				if (lang == "vi")
				{
					textToSpeak = originalText;
				}
				else 
				{
					var translatedDescription = poi.ResolveDescription(lang);
					if (translatedDescription != originalText && !string.IsNullOrWhiteSpace(translatedDescription))
					{
						textToSpeak = translatedDescription;
						logger.LogInformation("Using pre-translated text from POI data");
					}
					else if (!string.IsNullOrWhiteSpace(originalText))
					{
						logger.LogInformation("No pre-translation found. Translating via API (Slow)...");
						var translated = await TranslateTextAsync(originalText, "vi", lang, apiRootTrimmed, ct);
						if (!string.IsNullOrWhiteSpace(translated))
						{
							textToSpeak = translated;
						}
						else
						{
							logger.LogWarning("Translation failed, falling back to original");
							textToSpeak = originalText;
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(textToSpeak))
				{
					try
					{
						logger.LogInformation("Using device TTS");
						var locale = await PickLocaleAsync(lang, ct);
						if (locale != null)
						{
							var options = new SpeechOptions { Locale = locale, Volume = 1f, Pitch = 1f, Rate = 0.92f };
							await TextToSpeech.Default.SpeakAsync(textToSpeak, options, ct);
						}
						else
						{
							await TextToSpeech.Default.SpeakAsync(textToSpeak, cancelToken: ct);
						}
						audioPlayed = true;
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "TTS failed");
					}
				}
			}

			if (!audioPlayed)
			{
				logger.LogWarning("No audio URL and no text available for POI {PoiId}", poi.Id);
			}
		}
		finally
		{
			_gate.Release();
		}

		// Chờ phát xong âm thanh (nếu là file MP3) mới nhả trạng thái IsPlaying
		if (_player != null)
		{
			while (_player.IsPlaying && !ct.IsCancellationRequested)
			{
				await Task.Delay(150, ct);
			}
		}
		return ElapsedListenSeconds(sw);
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
			if (_localeCache.TryGetValue(lang, out var cachedLocale)) return cachedLocale;

			var locales = await TextToSpeech.Default.GetLocalesAsync();
			var match = locales.FirstOrDefault(l => l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
				?? locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
			
			if (match != null) _localeCache[lang] = match;
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

			var response = await _httpClient.PostAsJsonAsync(url, payload, ct);

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
