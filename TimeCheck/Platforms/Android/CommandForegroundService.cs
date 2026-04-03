using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// Foreground service that keeps a persistent notification in the status bar.
/// Tapping "Speak Command" launches <see cref="SpeechCaptureActivity"/>.
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMicrophone,
         Label = "TimeCheck Companion")]
public class CommandForegroundService : Service
{
    public const string ChannelId = "timecheck_companion";
    public const int NotificationId = 1001;
    public const string ActionStart = "timecheck.companion.START";
    public const string ActionStop = "timecheck.companion.STOP";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        CreateNotificationChannel();

        var speakIntent = new Intent(this, typeof(SpeechCaptureActivity));
        speakIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

        var pendingFlags = Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            : PendingIntentFlags.UpdateCurrent;

        var speakPendingIntent = PendingIntent.GetActivity(this, 0, speakIntent, pendingFlags);

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("TimeCheck Companion")
            .SetContentText("Ready for voice commands")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .AddAction(Resource.Mipmap.appicon, "Speak Command", speakPendingIntent)
            .Build();

        // Android 10+ (API 29) requires passing the service type to StartForeground
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification,
                global::Android.Content.PM.ForegroundService.TypeMicrophone);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Companion Commands", NotificationImportance.Low)
            {
                Description = "TimeCheck hands-free command channel"
            };
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }
}
