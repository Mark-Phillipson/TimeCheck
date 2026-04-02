using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.QuickSettings;
using Java.Lang;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Quick Settings tile. Tapping it launches <see cref="SpeechCaptureActivity"/>.
/// Register the tile in Android Settings → Quick Settings → Edit after first install.
/// </summary>
[Service(
    Label = "Speak Command",
    Permission = "android.permission.BIND_QUICK_SETTINGS_TILE",
    Icon = "@mipmap/appicon",
    Exported = true)]
[IntentFilter(["android.service.quicksettings.action.QS_TILE"])]
public class CommandTileService : TileService
{
    public override void OnTileAdded() => UpdateTile();
    public override void OnStartListening() => UpdateTile();

    public override void OnClick()
    {
        if (IsLocked)
            UnlockAndRun(new LaunchRunnable(this));
        else
            LaunchSpeechCapture();
    }

    private void LaunchSpeechCapture()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(SpeechCaptureActivity));
        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

        if ((int)Build.VERSION.SdkInt >= 34)
        {
            var pendingFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var pi = PendingIntent.GetActivity(context, 0, intent, pendingFlags)!;
            StartActivityAndCollapse(pi);
        }
        else
        {
#pragma warning disable CA1422, CS0618
            StartActivityAndCollapse(intent);
#pragma warning restore CA1422, CS0618
        }
    }

    private void UpdateTile()
    {
        if (QsTile == null) return;
        QsTile.State = TileState.Active;
        QsTile.UpdateTile();
    }

    private sealed class LaunchRunnable : Java.Lang.Object, IRunnable
    {
        private readonly CommandTileService _service;
        public LaunchRunnable(CommandTileService service) => _service = service;
        public void Run() => _service.LaunchSpeechCapture();
    }
}
