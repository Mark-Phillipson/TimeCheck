using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views.Accessibility;
using TimeCheck.Services;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Android Accessibility Service for global navigation and scroll gestures.
/// After installing the app, enable this service in Android Settings →
/// Accessibility → TimeCheck Companion.
/// </summary>
[Service(
    Label = "TimeCheck Companion",
    Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE",
    Exported = true)]
[IntentFilter(["android.accessibilityservice.AccessibilityService"])]
[MetaData("android.accessibilityservice", Resource = "@xml/accessibility_service_config")]
public class CompanionAccessibilityService : AccessibilityService
{
    private static CompanionAccessibilityService? _instance;

    /// <summary>Returns the live service instance, or null when not connected.</summary>
    public static CompanionAccessibilityService? Instance => _instance;

    protected override void OnServiceConnected()
    {
        _instance = this;
    }

    public override void OnAccessibilityEvent(AccessibilityEvent? e) { }

    public override void OnInterrupt() { }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Whether this service implementation can attempt to detect other accessibility services
    /// such as Google Voice Access. This returns true when the service is connected.
    /// </summary>
    public bool CanDetectVoiceAccess() => _instance != null;

    /// <summary>
    /// Heuristic, best-effort detection whether Google Voice Access appears enabled on the device.
    /// </summary>
    public Task<bool> IsVoiceAccessEnabledAsync()
    {
        try
        {
            return Task.FromResult(AccessibilityStateMonitor.IsVoiceAccessEnabled(this));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> PerformNavigationAsync(string action)
    {
        GlobalAction? globalAction = action.ToLowerInvariant() switch
        {
            "back" => GlobalAction.Back,
            "home" => GlobalAction.Home,
            "recents" => GlobalAction.Recents,
            "notifications" => GlobalAction.Notifications,
            _ => null
        };

        if (globalAction is null)
            return Task.FromResult(false);

        return Task.FromResult(PerformGlobalAction(globalAction.Value));
    }

    public Task<bool> PerformScrollAsync(string direction)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.N)
            return Task.FromResult(false);

        var metrics = Resources?.DisplayMetrics;
        if (metrics == null) return Task.FromResult(false);

        float w = metrics.WidthPixels;
        float h = metrics.HeightPixels;

        var (sx, sy, ex, ey) = direction.ToLowerInvariant() switch
        {
            "up" => (w / 2, h * 0.7f, w / 2, h * 0.3f),
            "down" => (w / 2, h * 0.3f, w / 2, h * 0.7f),
            "left" => (w * 0.7f, h / 2, w * 0.3f, h / 2),
            "right" => (w * 0.3f, h / 2, w * 0.7f, h / 2),
            _ => (0f, 0f, 0f, 0f)
        };

        if (sx == 0 && sy == 0) return Task.FromResult(false);

        var path = new global::Android.Graphics.Path();
        path.MoveTo(sx, sy);
        path.LineTo(ex, ey);

        var stroke = new GestureDescription.StrokeDescription(path, 0, 300);
        var gesture = new GestureDescription.Builder().AddStroke(stroke).Build();

        var tcs = new TaskCompletionSource<bool>();
        DispatchGesture(gesture, new GestureDispatchCallback(tcs), null);
        return tcs.Task;
    }

    private sealed class GestureDispatchCallback : AccessibilityService.GestureResultCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;
        public GestureDispatchCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;
        public override void OnCompleted(GestureDescription? gestureDescription) => _tcs.TrySetResult(true);
        public override void OnCancelled(GestureDescription? gestureDescription) => _tcs.TrySetResult(false);
    }
}
