using Android.Content;
using Android.Media;
using Android.OS;

namespace TimeCheck.Platforms.Android;

public class VoiceAccessSuppressionHelper
{
    private readonly Context _context;
    private AudioManager? _audioManager;
    private AudioFocusRequest? _afRequest;
    private bool _hasFocus = false;

    public VoiceAccessSuppressionHelper(Context context)
    {
        _context = context;
        _audioManager = (AudioManager?)context.GetSystemService(Context.AudioService);
    }

    public bool RequestAudioFocus()
    {
        if (_audioManager == null)
            return false;

        // Use the legacy request across API levels for simplicity and bindings compatibility
            try
            {
                // Fully-qualify Stream enum to avoid System.IO.Stream ambiguity
                var result = _audioManager.RequestAudioFocus(null, global::Android.Media.Stream.Music, global::Android.Media.AudioFocus.GainTransient);
                _hasFocus = true;
                return _hasFocus;
            }
        catch
        {
            _hasFocus = false;
            return false;
        }
    }

    public void ReleaseAudioFocus()
    {
        if (_audioManager == null)
            return;

        if (!_hasFocus)
            return;

        try
        {
            try
            {
                _audioManager.AbandonAudioFocus(null);
            }
            catch { }
        }
        catch
        {
            // swallow any unexpected errors during cleanup
        }
        finally
        {
            _hasFocus = false;
        }
    }
}
