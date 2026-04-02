#if ANDROID
using Android.Content;
using Android.Media;

namespace VinhKhanh.App.Platforms.Android;

public class AudioFocusHelper : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
{
	private readonly AudioManager _audioManager;
	private AudioFocusRequestClass? _focusRequest;

	public AudioFocusHelper()
	{
		_audioManager = (AudioManager)Platform.AppContext.GetSystemService(Context.AudioService)!;
	}

	public bool RequestFocus()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var attributes = new AudioAttributes.Builder()
				.SetUsage(AudioUsageKind.Media)!
				.SetContentType(AudioContentType.Speech)!
				.Build();

			_focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.GainTransientMayDuck)
				.SetAudioAttributes(attributes)
				.SetAcceptsDelayedFocusGain(true)
				.SetOnAudioFocusChangeListener(this)
				.Build();

			var result = _audioManager.RequestAudioFocus(_focusRequest);
			return result == AudioFocusRequest.Granted;
		}
		else
		{
#pragma warning disable CS0618
			var result = _audioManager.RequestAudioFocus(
				this, global::Android.Media.Stream.Music, AudioFocus.GainTransientMayDuck);
			return result == AudioFocusRequest.Granted;
#pragma warning restore CS0618
		}
	}

	public void AbandonFocus()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26) && _focusRequest != null)
		{
			_audioManager.AbandonAudioFocusRequest(_focusRequest);
		}
		else
		{
#pragma warning disable CS0618
			_audioManager.AbandonAudioFocus(this);
#pragma warning restore CS0618
		}
	}

	public void OnAudioFocusChange(AudioFocus focusChange)
	{
		// Ducking is handled automatically by the system on modern Android when GainTransientMayDuck is used.
		// If we lose focus completely, we might pause, but for short TTS, MayDuck is sufficient.
	}
}
#endif
