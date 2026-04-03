using global::Android.App;
using global::Android.Content;
using global::Android.Media;
using global::Android.Views;
using TimeCheck.Models;
using TimeCheck.Services;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Executes allowlisted <see cref="DeviceAction"/> types on the Android device.
/// </summary>
public class AndroidActionExecutor : IActionExecutor
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "device.open_app",
        "device.open_url",
        "device.navigate",
        "device.scroll",
        "device.media",
    };

    private readonly IAccessibilityCommandService _accessibility;

    public AndroidActionExecutor(IAccessibilityCommandService accessibility)
    {
        _accessibility = accessibility;
    }

    public async Task<ActionExecutionResult> ExecuteAsync(DeviceAction action, CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.Contains(action.Type))
            return Fail($"Action type '{action.Type}' is not in the allowlist.");

        try
        {
            return action.Type.ToLowerInvariant() switch
            {
                "device.open_app" => OpenApp(action),
                "device.open_url" => OpenUrl(action),
                "device.navigate" => await NavigateAsync(action),
                "device.scroll" => await ScrollAsync(action),
                "device.media" => DispatchMediaKey(action),
                _ => Fail("Unhandled action type.")
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    // Well-known app name aliases → package name, for apps whose label doesn't match common speech
    private static readonly Dictionary<string, string> KnownPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"] = "com.android.chrome",
        ["google chrome"] = "com.android.chrome",
        ["youtube"] = "com.google.android.youtube",
        ["maps"] = "com.google.android.apps.maps",
        ["google maps"] = "com.google.android.apps.maps",
        ["gmail"] = "com.google.android.gm",
        ["photos"] = "com.google.android.apps.photos",
        ["google photos"] = "com.google.android.apps.photos",
        ["settings"] = "com.android.settings",
        ["camera"] = "com.sec.android.app.camera",
        ["messages"] = "com.google.android.apps.messaging",
        ["phone"] = "com.samsung.android.dialer",
        ["spotify"] = "com.spotify.music",
        ["netflix"] = "com.netflix.mediaclient",
        ["amazon music"] = "com.amazon.mp3",
        ["whatsapp"] = "com.whatsapp",
    };

    private static ActionExecutionResult OpenApp(DeviceAction action)
    {
        var name = GetParam(action, "name");
        if (string.IsNullOrWhiteSpace(name))
            return Fail("Missing 'name' parameter for device.open_app.");

        var context = global::Android.App.Application.Context;
        var pm = context.PackageManager;
        if (pm == null) return Fail("PackageManager unavailable.");

        // 1. Try known package map first
        if (KnownPackages.TryGetValue(name, out var knownPackage))
        {
            var knownIntent = pm.GetLaunchIntentForPackage(knownPackage);
            if (knownIntent != null)
            {
                knownIntent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(knownIntent);
                return new ActionExecutionResult { Success = true };
            }
        }

        // 2. Fuzzy label search — match if label contains search term OR search term contains label
        var packages = pm.GetInstalledApplications(global::Android.Content.PM.PackageInfoFlags.MetaData);
        foreach (var pkg in packages)
        {
            var label = pm.GetApplicationLabel(pkg)?.ToString() ?? string.Empty;
            if (label.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                var launchIntent = pm.GetLaunchIntentForPackage(pkg.PackageName ?? string.Empty);
                if (launchIntent != null)
                {
                    launchIntent.SetFlags(ActivityFlags.NewTask);
                    context.StartActivity(launchIntent);
                    return new ActionExecutionResult { Success = true };
                }
            }
        }

        // Fallback: Play Store search
        try
        {
            var storeIntent = new Intent(Intent.ActionView,
                global::Android.Net.Uri.Parse($"market://search?q={Uri.EscapeDataString(name)}"));
            storeIntent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(storeIntent);
            return new ActionExecutionResult { Success = true };
        }
        catch
        {
            return Fail($"App '{name}' not found and Play Store unavailable.");
        }
    }

    private static ActionExecutionResult OpenUrl(DeviceAction action)
    {
        var url = GetParam(action, "url");
        if (string.IsNullOrWhiteSpace(url)
            || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return Fail("Invalid or missing 'url' parameter; must start with http:// or https://.");
        }

        var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(url));
        intent.SetFlags(ActivityFlags.NewTask);
        global::Android.App.Application.Context.StartActivity(intent);
        return new ActionExecutionResult { Success = true };
    }

    private async Task<ActionExecutionResult> NavigateAsync(DeviceAction action)
    {
        if (!_accessibility.IsConnected)
            return Fail("Accessibility service not connected. Enable TimeCheck Companion in Android Accessibility Settings.");

        var nav = GetParam(action, "action") ?? string.Empty;
        return await _accessibility.PerformNavigationAsync(nav)
            ? new ActionExecutionResult { Success = true }
            : Fail($"Navigation action '{nav}' failed.");
    }

    private async Task<ActionExecutionResult> ScrollAsync(DeviceAction action)
    {
        if (!_accessibility.IsConnected)
            return Fail("Accessibility service not connected.");

        var dir = GetParam(action, "direction") ?? string.Empty;
        return await _accessibility.PerformScrollAsync(dir)
            ? new ActionExecutionResult { Success = true }
            : Fail($"Scroll '{dir}' failed.");
    }

    private static ActionExecutionResult DispatchMediaKey(DeviceAction action)
    {
        var mediaAction = GetParam(action, "action")?.ToLowerInvariant();
        var keycode = mediaAction switch
        {
            "play" or "pause" => Keycode.MediaPlayPause,
            "next" => Keycode.MediaNext,
            "previous" => Keycode.MediaPrevious,
            _ => Keycode.Unknown
        };

        if (keycode == Keycode.Unknown)
            return Fail($"Unknown media action '{mediaAction}'.");

        var am = (AudioManager?)global::Android.App.Application.Context.GetSystemService(Context.AudioService);
        am?.DispatchMediaKeyEvent(new KeyEvent(KeyEventActions.Down, keycode));
        am?.DispatchMediaKeyEvent(new KeyEvent(KeyEventActions.Up, keycode));

        return new ActionExecutionResult { Success = true };
    }

    private static string? GetParam(DeviceAction action, string key)
        => action.Params != null && action.Params.TryGetValue(key, out var val) ? val?.ToString() : null;

    private static ActionExecutionResult Fail(string message)
        => new() { Success = false, ErrorMessage = message };
}
