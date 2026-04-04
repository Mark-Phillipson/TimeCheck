using Android.Content;
using Android.Views.Accessibility;
using System.Linq;

namespace TimeCheck.Platforms.Android;

public static class AccessibilityStateMonitor
{
    /// <summary>
    /// Best-effort detection whether Google Voice Access appears enabled on the device.
    /// Returns true if an enabled accessibility service's package name contains "voiceaccess".
    /// This is heuristic and not a guaranteed system API for controlling other services.
    /// </summary>
    public static bool IsVoiceAccessEnabled(Context context)
    {
        try
        {
            var am = (AccessibilityManager)context.GetSystemService(Context.AccessibilityService);
            // use 0 to request any feedback types - binding constants vary between platforms
            var enabled = am.GetEnabledAccessibilityServiceList(0);
            if (enabled == null) return false;

            return enabled.Any(s => s.ResolveInfo?.ServiceInfo?.PackageName?.ToLowerInvariant().Contains("voiceaccess") == true
                                  || s.ResolveInfo?.ServiceInfo?.Name?.ToLowerInvariant().Contains("voiceaccess") == true);
        }
        catch
        {
            return false;
        }
    }
}
