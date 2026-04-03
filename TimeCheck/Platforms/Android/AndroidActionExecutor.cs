using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Views;
using Android.Util;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using TimeCheck.Models;
using TimeCheck.Services;

namespace TimeCheck.Platforms.Android
{
    public class AndroidActionExecutor : IActionExecutor
    {
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            if (action == null)
                return Fail("Null action");

            string type = action.Type ?? string.Empty;
            if (!AllowedTypes.Contains(type))
                return Fail("Action type not allowed: " + type);

            type = type.ToLowerInvariant();
            try
            {
                switch (type)
                {
                    case "device.open_app":
                        return OpenApp(action);
                    case "device.open_url":
                        return OpenUrl(action);
                    case "device.navigate":
                        return await NavigateAsync(action).ConfigureAwait(false);
                    case "device.scroll":
                        return await ScrollAsync(action).ConfigureAwait(false);
                    case "device.media":
                        return DispatchMediaKey(action);
                    default:
                        return Fail("Unhandled action type: " + type);
                }
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        // simple alias map
        private static readonly Dictionary<string, string> KnownPackages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "youtube", "com.google.android.youtube" },
            { "chrome", "com.android.chrome" },
            { "maps", "com.google.android.apps.maps" },
            { "gmail", "com.google.android.gm" },
            { "photos", "com.google.android.apps.photos" },
            { "spotify", "com.spotify.music" },
        };

        private static void VerboseToast(Context ctx, string msg)
        {
            try
            {
                Log.Debug("TimeCheck", msg);
                MainThread.BeginInvokeOnMainThread(() => Toast.MakeText(ctx, msg, ToastLength.Short)?.Show());
            }
            catch { }
        }

        private static ActionExecutionResult OpenApp(DeviceAction action)
        {
            string name = GetParam(action, "name");
            if (string.IsNullOrWhiteSpace(name))
                return Fail("Missing 'name' parameter.");

            Context ctx = global::Android.App.Application.Context;
            PackageManager pm = ctx?.PackageManager;
            if (pm == null)
                return Fail("PackageManager unavailable.");

            VerboseToast(ctx, "OpenApp: looking for '" + name + "'");

            // known package
            string known;
            if (KnownPackages.TryGetValue(name, out known) && !string.IsNullOrEmpty(known))
            {
                try
                {
                    Intent launch = pm.GetLaunchIntentForPackage(known);
                    if (launch != null)
                    {
                        launch.SetFlags(ActivityFlags.NewTask);
                        ctx.StartActivity(launch);
                        return new ActionExecutionResult { Success = true };
                    }

                    // global query fallback
                    Intent gi = new Intent(Intent.ActionMain);
                    gi.AddCategory(Intent.CategoryLauncher);
                    var list = pm.QueryIntentActivities(gi, 0);
                    if (list != null)
                    {
                        foreach (var ri in list)
                        {
                            var ai = ri.ActivityInfo;
                            if (ai != null && string.Equals(ai.PackageName, known, StringComparison.OrdinalIgnoreCase))
                            {
                                Intent e = new Intent(Intent.ActionMain);
                                e.AddCategory(Intent.CategoryLauncher);
                                e.SetComponent(new ComponentName(ai.PackageName, ai.Name));
                                e.SetFlags(ActivityFlags.NewTask);
                                ctx.StartActivity(e);
                                return new ActionExecutionResult { Success = true };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    VerboseToast(ctx, "Known-package launch attempt failed: " + ex.Message);
                }
            }

            // fuzzy search installed apps
            try
            {
                var apps = pm.GetInstalledApplications(PackageInfoFlags.MetaData);
                if (apps != null)
                {
                    foreach (var pkg in apps)
                    {
                        var lblObj = pm.GetApplicationLabel(pkg);
                        string label = lblObj != null ? lblObj.ToString() : string.Empty;
                        if (string.IsNullOrEmpty(label)) continue;
                        if (label.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Intent li = pm.GetLaunchIntentForPackage(pkg.PackageName);
                            if (li != null)
                            {
                                li.SetFlags(ActivityFlags.NewTask);
                                ctx.StartActivity(li);
                                return new ActionExecutionResult { Success = true };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VerboseToast(ctx, "Fuzzy search failed: " + ex.Message);
            }

            // play store fallback
            try
            {
                var uri = global::Android.Net.Uri.Parse("market://search?q=" + Uri.EscapeDataString(name));
                Intent store = new Intent(Intent.ActionView, uri);
                store.SetFlags(ActivityFlags.NewTask);
                ctx.StartActivity(store);
                return new ActionExecutionResult { Success = true };
            }
            catch (Exception ex)
            {
                return Fail("App not found: " + ex.Message);
            }
        }

        private static ActionExecutionResult OpenUrl(DeviceAction action)
        {
            string url = GetParam(action, "url");
            if (string.IsNullOrWhiteSpace(url)) return Fail("Missing url");
            Intent i = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(url));
            i.SetFlags(ActivityFlags.NewTask);
            global::Android.App.Application.Context.StartActivity(i);
            return new ActionExecutionResult { Success = true };
        }

        private async Task<ActionExecutionResult> NavigateAsync(DeviceAction action)
        {
            if (!_accessibility.IsConnected) return Fail("Accessibility not connected");
            string nav = GetParam(action, "action") ?? string.Empty;
            bool ok = await _accessibility.PerformNavigationAsync(nav).ConfigureAwait(false);
            return ok ? new ActionExecutionResult { Success = true } : Fail("Navigation failed");
        }

        private async Task<ActionExecutionResult> ScrollAsync(DeviceAction action)
        {
            if (!_accessibility.IsConnected) return Fail("Accessibility not connected");
            string dir = GetParam(action, "direction") ?? string.Empty;
            bool ok = await _accessibility.PerformScrollAsync(dir).ConfigureAwait(false);
            return ok ? new ActionExecutionResult { Success = true } : Fail("Scroll failed");
        }

        private static ActionExecutionResult DispatchMediaKey(DeviceAction action)
        {
            string media = GetParam(action, "action") ?? string.Empty;
            media = media.ToLowerInvariant();
            Keycode code = Keycode.Unknown;
            if (media == "play" || media == "pause") code = Keycode.MediaPlayPause;
            else if (media == "next") code = Keycode.MediaNext;
            else if (media == "previous") code = Keycode.MediaPrevious;
            if (code == Keycode.Unknown) return Fail("Unknown media action");
            var am = global::Android.App.Application.Context.GetSystemService(Context.AudioService) as AudioManager;
            if (am != null)
            {
                am.DispatchMediaKeyEvent(new KeyEvent(KeyEventActions.Down, code));
                am.DispatchMediaKeyEvent(new KeyEvent(KeyEventActions.Up, code));
            }
            return new ActionExecutionResult { Success = true };
        }

        private static string GetParam(DeviceAction action, string key)
        {
            if (action.Params != null && action.Params.TryGetValue(key, out var val))
                return val != null ? val.ToString() : null;
            return null;
        }

        private static ActionExecutionResult Fail(string message)
        {
            return new ActionExecutionResult { Success = false, ErrorMessage = message };
        }
    }
}
