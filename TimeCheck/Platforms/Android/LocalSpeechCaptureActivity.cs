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
using System.Text.RegularExpressions;
using System.Text.Json;
using Android.Util;
using TimeCheck.Platforms.Android;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Transparent activity that shows the Android speech recognition dialog and
/// executes any recognised device actions locally — no API request is made.
/// </summary>
[Activity(Theme = "@android:style/Theme.Translucent.NoTitleBar", Label = "Local Voice Command")]
public class LocalSpeechCaptureActivity : global::Android.App.Activity
{
    private const int SpeechRequestCode = 74;
    private const string PackagedLaunchesFileName = "local_launches.json";
    private VoiceAccessSuppressionHelper? _suppression;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            _suppression = new VoiceAccessSuppressionHelper(this);
        }
        catch (Exception ex)
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
        intent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak your local command…");
        intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

        try
        {
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

        Finish();

        try
        {
            _suppression?.ReleaseAudioFocus();
        }
        catch { }

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
        var lookupText = CommandPhraseParser.ExtractLookupText(text);

        DeviceAction? action = null;

        // Decide intent by verb: 'open' -> prefer app on device (with Play Store fallback), other verbs -> web/search
        var isOpenVerb = CommandPhraseParser.IsOpenVerb(text);
        var isExplicitUrlOpen = CommandPhraseParser.IsExplicitUrlOpen(text);

        // Prefer local launch mappings for any non-explicit URL command, including 'open'.
        // This lets custom entries (for example installed PWAs represented by URL launches)
        // win before generic open-app/Play Store fallback logic.
        if (!isExplicitUrlOpen)
        {
            try
            {
                LaunchRecord? match = null;
                if (launchService != null)
                {
                    foreach (var candidate in BuildLaunchLookupCandidates(text))
                    {
                        match = await launchService.FindBestMatchAsync(candidate);
                        if (match != null)
                        {
                            if (!string.Equals(candidate, text, StringComparison.OrdinalIgnoreCase))
                                global::Android.Util.Log.Debug("TimeCheck", $"Launch lookup normalized from '{text}' to '{candidate}'");
                            break;
                        }
                    }
                }

                if (match == null && launchService != null)
                {
                    var launches = await launchService.GetLocalLaunchesAsync();
                    match = TryFindLooseLaunchMatch(launches, text);
                }

                // Final fallback: read bundled launch mappings directly from app package.
                // This avoids false web/Play Store fallbacks when service cache misses.
                if (match == null)
                {
                    var packagedLaunches = await TryLoadPackagedLaunchesAsync();
                    if (packagedLaunches.Count > 0)
                    {
                        match = TryFindLooseLaunchMatch(packagedLaunches, text);
                        if (match != null)
                            global::Android.Util.Log.Debug("TimeCheck", $"Packaged launch matched: {match.Name} ({match.VoiceKey})");
                    }
                }

                if (match != null && !string.IsNullOrWhiteSpace(match.Url))
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
            if (isExplicitUrlOpen)
            {
                var url = text;
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    url = "https://" + url.Replace("open ", "").Replace("go to ", "");

                action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = url } };
            }
            // 'open' verb: prefer to open an app on device; if it fails, open Google Play search for the app name
            else if (isOpenVerb)
            {
                var name = lookupText;

                // Try to open the app immediately; if that fails, fallback to Play Store search
                try
                {
                    var tryAction = new DeviceAction { Type = "device.open_app", Params = new Dictionary<string, string> { ["name"] = name } };
                    var tryResult = await executor.ExecuteAsync(tryAction);
                    if (tryResult != null && tryResult.Success)
                    {
                        ShowToast($"Opened {name}...");
                        return;
                    }
                    else
                    {
                        // If phrase looks web-oriented, use browser search instead of Play Store.
                        if (LooksLikeWebIntent(name))
                        {
                            var webSearch = $"https://www.google.com/search?q={System.Uri.EscapeDataString(name)}";
                            action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = webSearch } };
                        }
                        else
                        {
                            var playSearch = $"https://play.google.com/store/search?q={System.Uri.EscapeDataString(name)}&c=apps";
                            action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = playSearch } };
                        }
                    }
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Debug("TimeCheck", $"Open-app attempt failed: {ex.Message}");
                    if (LooksLikeWebIntent(name))
                    {
                        var webSearch = $"https://www.google.com/search?q={System.Uri.EscapeDataString(name)}";
                        action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = webSearch } };
                    }
                    else
                    {
                        var playSearch = $"https://play.google.com/store/search?q={System.Uri.EscapeDataString(name)}&c=apps";
                        action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = playSearch } };
                    }
                }
            }
            // Other verbs like 'launch', 'start', 'browse', 'search' -> treat as web/open browser search
            else if (CommandPhraseParser.IsSearchIntentVerb(text))
            {
                var keyword = lookupText;

                // If a local launch matched earlier and provided a URL, prefer it; otherwise open browser search
                if (action == null)
                {
                    var searchUrl = $"https://www.google.com/search?q={System.Uri.EscapeDataString(keyword)}";
                    action = new DeviceAction { Type = "device.open_url", Params = new Dictionary<string, string> { ["url"] = searchUrl } };
                }
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

    private static bool LooksLikeWebIntent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var lower = value.ToLowerInvariant();
        return lower.Contains(" site")
            || lower.Contains(" website")
            || lower.Contains(" web ")
            || lower.Contains(" page")
            || lower.Contains(".com")
            || lower.Contains(".co")
            || lower.Contains(".net")
            || lower.Contains(".org");
    }

    private static IEnumerable<string> BuildLaunchLookupCandidates(string phrase)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var yieldReturn = new List<string>();

        void Add(string value)
        {
            var cleaned = CommandPhraseParser.NormalizeWhitespace(value);
            if (!string.IsNullOrWhiteSpace(cleaned) && seen.Add(cleaned))
                yieldReturn.Add(cleaned);
        }
        Add(phrase);
        Add(CommandPhraseParser.ExtractLookupText(phrase));

        var replacementPairs = new (string from, string to)[]
        {
            ("blazer", "blazor"),
            ("blasor", "blazor"),
            ("blazor", "blazer"),
            ("zite", "site"),
            ("sight", "site"),
            ("website", "site"),
            ("web site", "site")
        };

        var seedValues = yieldReturn.ToArray();
        foreach (var seed in seedValues)
        {
            foreach (var (from, to) in replacementPairs)
            {
                var pattern = $@"\b{Regex.Escape(from)}\b";
                var replaced = Regex.Replace(seed, pattern, to, RegexOptions.IgnoreCase);
                Add(replaced);
                Add(CommandPhraseParser.ExtractLookupText(replaced));
            }
        }

        return yieldReturn;
    }

    private static LaunchRecord? TryFindLooseLaunchMatch(IEnumerable<LaunchRecord> launches, string phrase)
    {
        var candidates = BuildLaunchLookupCandidates(phrase)
            .Select(NormalizeForLooseMatch)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        LaunchRecord? best = null;
        var bestScore = 0;

        foreach (var launch in launches)
        {
            if (!launch.IsLocal)
                continue;

            var values = new List<string>();
            if (!string.IsNullOrWhiteSpace(launch.Name)) values.Add(launch.Name);
            if (!string.IsNullOrWhiteSpace(launch.VoiceKey)) values.Add(launch.VoiceKey);
            if (launch.Aliases != null) values.AddRange(launch.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)));

            var normalizedValues = values
                .Select(NormalizeForLooseMatch)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var localScore = 0;
            foreach (var query in candidates)
            {
                foreach (var value in normalizedValues)
                {
                    if (string.Equals(query, value, StringComparison.OrdinalIgnoreCase))
                        localScore = Math.Max(localScore, 100);
                    else if (value.Contains(query, StringComparison.OrdinalIgnoreCase) && query.Length >= 4)
                        localScore = Math.Max(localScore, 80);
                    else if (query.Contains(value, StringComparison.OrdinalIgnoreCase) && value.Length >= 4)
                        localScore = Math.Max(localScore, 70);
                }
            }

            if (localScore > bestScore)
            {
                bestScore = localScore;
                best = launch;
            }
        }

        return bestScore >= 70 ? best : null;
    }

    private static string NormalizeForLooseMatch(string value)
    {
        var normalized = value.ToLowerInvariant();
        normalized = Regex.Replace(normalized, "[^a-z0-9 ]", " ");
        normalized = Regex.Replace(normalized, "\\s+", " ").Trim();
        return normalized;
    }

    private static async Task<IReadOnlyList<LaunchRecord>> TryLoadPackagedLaunchesAsync()
    {
        try
        {
            using var stream = await FileSystem.Current.OpenAppPackageFileAsync(PackagedLaunchesFileName);
            using var reader = new System.IO.StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<LaunchRecord>();

            var deserialized = JsonSerializer.Deserialize<List<LaunchRecord>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return deserialized ?? new List<LaunchRecord>();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Debug("TimeCheck", $"Packaged launch load failed: {ex.Message}");
            return Array.Empty<LaunchRecord>();
        }
    }
}
