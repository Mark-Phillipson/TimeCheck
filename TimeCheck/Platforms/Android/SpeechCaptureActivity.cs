using Android.App;
using Android.Content;
using Android.OS;
using Android.Speech;
using TimeCheck.Models;
using TimeCheck.Services;
using Android.Content.Res;
using Android.Runtime;
using Android.Util;

// Audio focus helper
using TimeCheck.Platforms.Android;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Transparent activity that shows the Android speech recognition dialog,
/// sends the result to the assistant API, then executes any returned actions.
/// </summary>
[Activity(Theme = "@android:style/Theme.Translucent.NoTitleBar", Label = "Voice Command")]
public class SpeechCaptureActivity : global::Android.App.Activity
{
    private const int SpeechRequestCode = 73;
    private VoiceAccessSuppressionHelper? _suppression;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            _suppression = new VoiceAccessSuppressionHelper(this);
        }
        catch (System.Exception ex)
        {
            Log.Debug("TimeCheck", $"Suppression helper init failed: {ex.Message}");
            _suppression = null;
        }
        StartSpeechRecognition();
    }

    private void StartSpeechRecognition()
    {
        var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
        intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
        intent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak your command…");
        intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

        try
        {
            // Only request audio focus if the user has enabled interference reduction in settings
            var services = IPlatformApplication.Current?.Services;
            var settings = services?.GetService<TimeCheck.Services.ISettingsService>();
            try { settings?.Load(); } catch { }
            if (settings == null || settings.UseInterferenceReduction)
                _suppression?.RequestAudioFocus();

            StartActivityForResult(intent, SpeechRequestCode);
        }
        catch (ActivityNotFoundException)
        {
            _suppression?.ReleaseAudioFocus();
            ShowToast("Speech recognition not available on this device.");
            Finish();
        }
    }

    protected override void OnActivityResult(int requestCode, global::Android.App.Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        string? spokenText = null;
        if (requestCode == SpeechRequestCode && resultCode == global::Android.App.Result.Ok)
        {
            var results = data?.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
            spokenText = results?.Count > 0 ? results[0] : null;
        }

        // Dismiss the transparent activity immediately so the app remains responsive
        Finish();

        // Always release audio focus when we're finished
        try
        {
            _suppression?.ReleaseAudioFocus();
        }
        catch { }

        if (!string.IsNullOrWhiteSpace(spokenText))
            _ = SendCommandAsync(spokenText);
    }

    private async Task SendCommandAsync(string command)
    {
        var services = IPlatformApplication.Current?.Services;
        var apiClient = services?.GetService<IAssistantApiClient>();
        var settings = services?.GetService<ISettingsService>();
        var executor = services?.GetService<IActionExecutor>();

        if (apiClient == null || settings == null)
        {
            ShowToast("Assistant not configured.");
            return;
        }

        settings.Load();

        if (string.IsNullOrWhiteSpace(settings.AssistantBaseUrl))
        {
            ShowToast("Assistant URL not set. Configure it in the app.");
            return;
        }

        try
        {
            ShowToast($"Sending: {command}");

            var request = new CommandRequest
            {
                Command = command,
                DeviceToken = settings.DeviceToken,
                DeviceName = settings.DeviceName
            };

            var response = await apiClient.SendCommandAsync(request);
            ShowToast(response.TextResponse);

            if (executor != null && response.Actions?.Count > 0)
            {
                foreach (var action in response.Actions)
                    await executor.ExecuteAsync(action);
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Error: {ex.Message}");
        }
    }

    private void ShowToast(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            global::Android.Widget.Toast.MakeText(this, message, global::Android.Widget.ToastLength.Long)?.Show());
    }
}
