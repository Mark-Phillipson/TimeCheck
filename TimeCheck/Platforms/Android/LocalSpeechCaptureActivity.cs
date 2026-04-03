using Android.App;
using Android.Content;
using Android.OS;
using Android.Speech;
using TimeCheck.Models;
using TimeCheck.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Transparent activity that shows the Android speech recognition dialog and
/// executes any recognised device actions locally — no API request is made.
/// </summary>
[Activity(Theme = "@android:style/Theme.Translucent.NoTitleBar", Label = "Local Voice Command")]
public class LocalSpeechCaptureActivity : global::Android.App.Activity
{
    private const int SpeechRequestCode = 74;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        StartSpeechRecognition();
    }

    private void StartSpeechRecognition()
    {
        var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
        intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
        intent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak your local command…");
        intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

        try
        {
            StartActivityForResult(intent, SpeechRequestCode);
        }
        catch (ActivityNotFoundException)
        {
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

        Finish();

        if (!string.IsNullOrWhiteSpace(spokenText))
            _ = ExecuteLocallyAsync(spokenText);
    }

    private async Task ExecuteLocallyAsync(string recognisedText)
    {
        ShowToast($"Heard: {recognisedText}");

        var services = IPlatformApplication.Current?.Services;
        var executor = services?.GetService<IActionExecutor>();
        var launchService = services?.GetService<ILaunchService>();

        if (executor == null)
        {
            ShowToast("No local executor available.");
            return;
        }

        var text = recognisedText.Trim();
        var lower = text.ToLowerInvariant();

        DeviceAction? action = null;

        // FIRST: allow local launches configured in the LaunchService to override generic handling
        if (launchService != null)
        {
            try
            {
                var match = await launchService.FindBestMatchAsync(recognisedText);
                if (match != null)
                {
                    global::Android.Util.Log.Debug("TimeCheck", $"Local launch matched: {match.Name} ({match.VoiceKey})");
                    ShowToast($"Opening {match.Name}...");
                    action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = match.Url } };
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Debug("TimeCheck", $"LaunchService error: {ex.Message}");
            }
        }

        // If no local match, continue with generic heuristics
        if (action == null)
        {
            // Open URL (explicit)
            if (lower.StartsWith("open http") || lower.StartsWith("open https") || lower.StartsWith("go to ") || lower.StartsWith("open www."))
            {
                var url = text;
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    url = "https://" + url.Replace("open ", "").Replace("go to ", "");

                action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = url } };
            }
            // Open app
            else if (lower.StartsWith("open ") || lower.StartsWith("launch ") || lower.StartsWith("start "))
            {
                var name = text.Substring(text.IndexOf(' ') + 1).Trim();
                action = new DeviceAction { Type = "device.open_app", Params = new Dictionary<string, string> { ["name"] = name } };
            }
            
            // Media controls
            else if (lower.Contains("play") || lower.Contains("pause") || lower.Contains("next") || lower.Contains("previous") || lower.Contains("skip"))
            {
                string media = "";
                if (lower.Contains("play") && !lower.Contains("pause")) media = "play";
                else if (lower.Contains("pause")) media = "pause";
                else if (lower.Contains("next") || lower.Contains("skip")) media = "next";
                else if (lower.Contains("previous") || lower.Contains("back")) media = "previous";

                if (!string.IsNullOrEmpty(media))
                    action = new DeviceAction { Type = "device.media", Params = new Dictionary<string, string> { ["action"] = media } };
            }
            // Scroll
            else if (lower.StartsWith("scroll ") || lower.StartsWith("swipe "))
            {
                var parts = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var dir = parts[1];
                    action = new DeviceAction { Type = "device.scroll", Params = new Dictionary<string, string> { ["direction"] = dir } };
                }
            }
            // Navigate
            else if (lower.Contains("go back") || lower == "back")
            {
                action = new DeviceAction { Type = "device.navigate", Params = new Dictionary<string, string> { ["action"] = "back" } };
            }
            else if (lower.Contains("go home") || lower == "home")
            {
                action = new DeviceAction { Type = "device.navigate", Params = new Dictionary<string, string> { ["action"] = "home" } };
            }
            else if (lower.Contains("recents") || lower.Contains("recent apps") || lower.Contains("show recent"))
            {
                action = new DeviceAction { Type = "device.navigate", Params = new Dictionary<string, string> { ["action"] = "recents" } };
            }
            else if (lower.Contains("notification") || lower.Contains("show notifications") || lower.Contains("open notifications"))
            {
                action = new DeviceAction { Type = "device.navigate", Params = new Dictionary<string, string> { ["action"] = "notifications" } };
            }

        }

        if (action == null)
        {
            ShowToast("No local action recognised for that phrase.");
            return;
        }

        try
        {
            var result = await executor.ExecuteAsync(action);
            if (result != null && result.Success)
                ShowToast("Local action executed successfully.");
            else
                ShowToast($"Action failed: {result?.ErrorMessage ?? "unknown error"}");
        }
        catch (Exception ex)
        {
            ShowToast($"Execution error: {ex.Message}");
        }
    }

    private void ShowToast(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            global::Android.Widget.Toast.MakeText(this, message, global::Android.Widget.ToastLength.Long)?.Show());
    }
}
