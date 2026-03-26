using System.Diagnostics;
using Plugin.Maui.Audio;
using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

public sealed class NarrationService(IAudioManager audioManager) : INarrationService
{
	private IAudioPlayer? _player;
	private MemoryStream? _playbackStream;
	private readonly SemaphoreSlim _gate = new(1, 1);

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
		// Convert Poi to PoiSnapshot and use existing PlayPoiAsync method
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
			AudioViUrl = poi.AudioViUrl
		};
		
		var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
		return PlayPoiAsync(poiSnapshot, language, apiRoot);
	}

	public Task StopCurrentAsync() => StopAsync();

	/// <summary>Phat thuyet minh; tra ve thoi luong nghe uoc tinh (giay) cho analytics.</summary>
	public async Task<int> PlayPoiAsync(PoiSnapshot poi, string lang, string apiRootTrimmed, CancellationToken ct = default)
	{
		await _gate.WaitAsync(ct);
		var sw = Stopwatch.StartNew();
		try
		{
			await StopAsync();

			var audioUrl = poi.ResolveAudioUrl(lang);
			var text = poi.ResolveDescription(lang);

			if (!string.IsNullOrWhiteSpace(audioUrl))
			{
				var abs = audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
					? audioUrl
					: $"{apiRootTrimmed.TrimEnd('/')}/{audioUrl.TrimStart('/')}";

				try
				{
					using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
					var bytes = await http.GetByteArrayAsync(abs, ct);
					var ext = Path.GetExtension(new Uri(abs).AbsolutePath);
					if (string.IsNullOrEmpty(ext)) ext = ".mp3";
					_playbackStream = new MemoryStream(bytes);
					_player = audioManager.CreatePlayer(_playbackStream);
					_player.Play();
					while (_player.IsPlaying && !ct.IsCancellationRequested)
						await Task.Delay(150, ct);
					return ElapsedListenSeconds(sw);
				}
				catch
				{
					/* fallback TTS */
				}
			}

			if (!string.IsNullOrWhiteSpace(text))
			{
				var locale = await PickLocaleAsync(lang, ct);
				await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
				{
					Locale = locale,
					Volume = 1f,
					Pitch = 1f,
					Rate = 0.92f
				}, ct);
				return ElapsedListenSeconds(sw);
			}
		}
		finally
		{
			_gate.Release();
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
		var locales = await TextToSpeech.Default.GetLocalesAsync();
		return locales.FirstOrDefault(l => l.Language.StartsWith(lang, StringComparison.OrdinalIgnoreCase))
		       ?? locales.FirstOrDefault(l => l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
	}
}
