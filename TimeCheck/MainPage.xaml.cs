// MainPage.xaml.cs
#if ANDROID
using Android.Speech.Tts;
using Android.OS;
using Android.Content;
using Java.Lang;
using Microsoft.Maui.ApplicationModel;
using Android.Runtime;
#endif

using Microsoft.Maui.Controls;
using System;
using TimeCheck.Models;
using TimeCheck.Services;

namespace TimeCheck
{    public partial class MainPage : ContentPage
    {
#if ANDROID
        private Android.Speech.Tts.TextToSpeech? _tts;
        private bool _ttsReady = false;
#endif        // Mode management
        private enum Mode { TimeCheck, Cycling }
        private Mode _currentMode = Mode.TimeCheck; // Start with time check mode

        private readonly ISettingsService _settingsService;
        private readonly IAssistantApiClient _assistantApiClient;

        private readonly List<string> _cyclingEncouragements = new List<string>
        {
            "Move it, you splendid sod — pedal like the sergeant's watching!",
            "Put some bleeding effort into it, you horrible little man!",
            "Eyes front, legs turning — show those tarmac traitors who's boss!",
            "Pick up the pace, you dawdling peacock!",
            "Don't wheeze like a pensioner; give it some welly!",
            "You're not on a Sunday stroll — pedal like it's a route march!",
            "Shift that backside and make those pedals pay attention!",
            "Come on, you glorious wreck, churn those gears!",
            "Legs like pistons, soldier — get them firing!",
            "If the sergeant heard that wheeze he'd have you doing laps!",
            "Stop admiring the scenery and start punishing the road!",
            "Waste not a breath moaning — burn it into forward motion!",
            "Bend metal with your thighs, you magnificent nuisance!",
            "Don't be a biscuit — pedal like someone stole your tea!",
            "One more push and you'll be less pathetic and more presentable!",
            "Keep it moving, you daft mariner of the road!",
            "Slog through it — the hill's just showing off, not you!",
            "Don't be a limp noodle; be a proper bit of kit!",
            "Pedal like you put a bet on your finish time!",
            "No dawdling — the road doesn't care about your excuses!",
            "Sound off with your legs, not your complaints!",
            "Give it the beans, you marvelous underachiever!",
            "Quit moaning and let your wheels do the talking!",
            "Harden up and pedal, — charm is strictly optional!",
            "Stop faffing around and make that incline regret its choices!",
            "Sweat like a saint and pedal like a sinner caught stealing!",
            "Pull yourself together and show that hill no mercy!",
            "Legs on fire? Good — that's improvement cooking!",
            "Mind over gearbox — think hard, pedal harder!",
            "You're nearly there, you stubborn bit of brilliance!",
            "Keep going — this isn't supposed to be easy, darling!",
            "Hustle up, you caffeine-fuelled battalion of one!",
            "If you slow now you'll only have to face the shame later!",
            "Charge like a confused cavalryman — full speed, less thinking!",
            "Move like you mean it, and mean it loudly!",
            "Stop being polite to the hill — it's rude enough already!",
            "This isn't a promenade — it's a proving ground!",
            "Pedal like you've misplaced your dignity and found it downhill!",
            "Be the nuisance the hill never asked for!",
            "Get on with it — the tarmac won't applaud, but you'll know!",
            "Hurry up, you magnificent so-and-so, and keep those legs honest!",
            "Power through like a bloke with a point to prove!",
            "Don't let the hill have the last laugh — pedal louder!",
            "If your legs could speak they'd apologise for the noise. Make them proud!",
            "Stop looking for sympathy — the road gives none!",
            "Give 'em hell and call it an interval session!",
            "Pedal like you owe the crown money and they're coming to collect!",
            "Hurry up — the next village won't wait for your theatrics!",
            "When in doubt, stand on the pedals and swear at the incline!",
            "Put a bit of elbow grease into those pedals, why don't you!",
            "Muster some grit and show that slope who's boss!",
            "Don't be meeker than a mouse in parade rest — push!",
            "Squeeze the road for all it's worth; there's no refund!",
            "Act like it's the last mile of the parade — loud and proud!",
            "Leg power now, excuses at the pub later!",
            "You're nearly earning your bragging rights — don't squander them!",
            "Give it a right old go, you splendidly misdirected soul!",
            "If you can grumble, you can pedal harder — start doing both!",
            "Pretend you're late for tea — nothing gets you going like that!",
            "Push like a corporal with a stopwatch — efficient and noisy!",
            "Stop being delicate; be a proper, slightly sweaty legend!",
            "Drive those pedals like they're enemy territory!",
            "Don't just roll — dominate the rotation!",
            "Look fierce, pedal fiercer — psychological warfare, that is!",
            "If your legs had medals, they'd be heavy by now — earn 'em!",
            "Don't give the hill satisfaction — take it for yourself!",
            "Act like you trained for this in a shed and keep proving it!",
            "Throw some oomph into it — your bike needs moral support!",
            "Keep the cadence up; lethargy is for someone else's ride!",
            "Stride those pedals with the stubbornness of a mule and the grace of a drunk dancer!",
            "Put the boot in, metaphorically and with your calves!",
            "Imagine the hill's your ex — pass it without apology!",
            "Don't ask for easy; ask for more pedals and less complaining!",
            "Act like this is training for something mysterious and important!",
            "You're not here to look pretty, you're here to get up the hill!",
            "Stand up, push down, and swear softly at your inner critic!",
            "Remember: sweat is just proof you've been brutally honest with yourself!",
            "Be the sort of cyclist that makes the hill reconsider its life choices!",
            "Push like a man who knows the pub shutters close soon!",
            "Pedal like you nicked somebody's sandwich and need to get away!",
            "Keep going — half-hearted effort is for vegetables!",
            "Treat the hill like a minor annoyance and ride it out!",
            "You look better in motion; keep the show on the road!",
            "Grin like a soldier, pedal like a machine — results follow!",
            "Pedal with intent or at least with good posture!",
            "Show that gradient you have a spine of iron and a sense of humour!",
            "Don't be a spectator in your own ride — be the event!",
            "Finish this climb and call it a character-building exercise!",
            "Legs, meet challenge. Challenge, meet relentless persistence!",
            "When your legs scream, that's just applause from the future you!",
            "Storm that summit like it's a particularly loud drum!",
            "Be ridiculous, be brave, be sweaty — and keep pedalling!",
            "You've got the kit and the cheek — now use both!",
            "Make this climb regret ever daring to stand in your way!",
            "Now pedal, you glorious incompetent — make it count!"
        };

        
        private readonly Random _random = new Random();
        private readonly double _encMinMinutes = 1.0; // minimum random interval in minutes
        private readonly double _encMaxMinutes = 10.0; // maximum random interval in minutes

        public MainPage(ISettingsService settingsService, IAssistantApiClient assistantApiClient)
        {
            _settingsService = settingsService;
            _assistantApiClient = assistantApiClient;

            InitializeComponent();
            SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object? sender, EventArgs e)
        {
            // Temporarily disable landscape hiding to ensure buttons are always visible
            // TODO: Re-enable landscape optimization later if needed
            /*
            // Only hide buttons in landscape mode when we have a significant width advantage
            // and reasonable dimensions (to avoid hiding on startup when dimensions might be 0)
            bool isLandscape = Width > 0 && Height > 0 && Width > Height && (Width / Height) > 1.3;
            
            MinimizeAppButton.IsVisible = !isLandscape;
            CloseAppButton.IsVisible = !isLandscape;
            TimeCheckModeButton.IsVisible = !isLandscape;
            EncouragementModeButton.IsVisible = !isLandscape;
            CurrentModeLabel.IsVisible = !isLandscape;
            */
            
            // Adjust time label font size based on orientation
            bool isLandscape = Width > Height;
            TimeLabel.FontSize = isLandscape ? 60 : 96;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            HelpLabel.Text = "Tap button to hear time or encouragement based on current mode.";
            
            // Ensure toggle buttons are visible initially
            TimeCheckModeButton.IsVisible = true;
            EncouragementModeButton.IsVisible = true;
            CurrentModeLabel.IsVisible = true;
            
            UpdateModeDisplay(); // Initialize mode display
            StartMinuteTimer();

            _settingsService.Load();
            AssistantUrlEntry.Text = _settingsService.AssistantBaseUrl;
            DeviceTokenEntry.Text = _settingsService.DeviceToken;
            DeviceNameEntry.Text = _settingsService.DeviceName;

            // Auto-start the companion service if settings are configured
            if (!_companionServiceRunning
                && !string.IsNullOrWhiteSpace(_settingsService.AssistantBaseUrl)
                && !string.IsNullOrWhiteSpace(_settingsService.DeviceToken))
            {
                _ = StartCompanionServiceAsync();
            }
            else
            {
                AssistantStatusLabel.Text = _companionServiceRunning
                    ? "Companion service running."
                    : "Enter URL and token, then save to auto-start service.";
            }

#if ANDROID
            if (_tts == null)
            {
                var activity = Platform.CurrentActivity;
                if (activity != null)
                {
                    _tts = new Android.Speech.Tts.TextToSpeech(activity, new TtsInitListener(this));
                }
                else
                {
                    HelpLabel.Text = "Text-to-speech activity not available yet.";
                }
            }
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // No timer to stop/dispose
        }

        private void UpdateTimeLabel()
        {
            var currentTime = DateTime.Now.ToString("hh:mm");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimeLabel.Text = currentTime;
                HelpLabel.Text = "Last updated: " + DateTime.Now.ToString("h:mm:ss tt 'on' dddd, MMMM dd, yyyy");
            });
        }        private void StartMinuteTimer()
        {
            // Update every minute for the clock display
            Dispatcher.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                UpdateTimeLabel();
                return true; // Repeat every minute
            });
            // Also update immediately on load
            UpdateTimeLabel();

            // Time Check Mode: Say the time every 5 minutes (3 times)
            Dispatcher.StartTimer(TimeSpan.FromMinutes(5), () =>
            {
                if (_currentMode == Mode.TimeCheck)
                {
                    SayTime();
                }
                return true; // Repeat every 5 minutes
            });

            // Encouragement Mode: schedule encouragements at random intervals
            ScheduleNextEncouragement();
        }

        private void ScheduleNextEncouragement()
        {
            // Pick a random delay between min and max minutes (fractional allowed)
            double minutes = _random.NextDouble() * (_encMaxMinutes - _encMinMinutes) + _encMinMinutes;
            var delay = TimeSpan.FromMinutes(minutes);

            // Optionally show next scheduled time in the help label
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HelpLabel.Text = $"Next encouragement in {System.Math.Round(minutes, 1)} minutes.";
            });

            Dispatcher.StartTimer(delay, () =>
            {
                if (_currentMode == Mode.Cycling)
                {
                    SayEncouragement();
                }
                // Schedule the following encouragement (recursive scheduling)
                ScheduleNextEncouragement();
                return false; // don't repeat this timer — we've rescheduled
            });
        }

        private void SayTime()
        {
            UpdateTimeLabel();
#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
            try
            {
                var currentTime = DateTime.Now.ToString("h:mm tt");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                        for (int i = 0; i < 3; i++)
                        {
                            var stream = await synthesizer.SynthesizeTextToStreamAsync($"The time is {currentTime}");
                            var mediaPlayer = new Windows.Media.Playback.MediaPlayer();
                            mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType);
                            mediaPlayer.Play();
                            // Wait for the speech to finish before repeating
                            await Task.Delay(2500); // Adjust delay as needed for clarity
                        }
                    }
                    catch (System.Exception ex)
                    {
                        HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
                    }
                });
            }
            catch (System.Exception ex)
            {
                HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
            }
#elif ANDROID
            try
            {
                if (_tts != null && _ttsReady)
                {
                    var currentTime = DateTime.Now.ToString("h:mm tt");
                    for (int i = 0; i < 3; i++)
                    {
                        // Use the modern Bundle overload on Lollipop+ to avoid deprecated IDictionary overloads
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                        {
                            var bundle = new Android.OS.Bundle();
                            _tts.Speak($"The time is {currentTime}", Android.Speech.Tts.QueueMode.Add, bundle, $"utteranceId_{i}");
                        }
                        else
                        {
                            // Fallback for very old devices — use the older overload (rare path)
#pragma warning disable CS0618
                            _tts.Speak($"The time is {currentTime}", Android.Speech.Tts.QueueMode.Add, null);
#pragma warning restore CS0618
                        }
                    }
                }
                else if (_tts == null)
                {
                    HelpLabel.Text = "Text-to-speech service is not initialized.";
                }
                else if (!_ttsReady)
                {
                    HelpLabel.Text = "Text-to-speech not ready. Please try again in a moment.";
                }
            }
            catch (System.Exception ex)
            {
                HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
            }
#else
            HelpLabel.Text = "Text-to-speech is not supported on this platform.";
#endif
        }        private void StartListeningButton_Clicked(object sender, EventArgs e)
        {
            if (_currentMode == Mode.TimeCheck)
            {
                SayTime();
            }
            else
            {
                SayEncouragement();
            }
        }

        private void SayEncouragement()
        {
            string encouragement = _cyclingEncouragements[_random.Next(_cyclingEncouragements.Count)];
            
#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                        var stream = await synthesizer.SynthesizeTextToStreamAsync(encouragement);
                        var mediaPlayer = new Windows.Media.Playback.MediaPlayer();
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType);
                        mediaPlayer.Play();
                    }
                    catch (System.Exception ex)
                    {
                        HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
                    }
                });
            }
            catch (System.Exception ex)
            {
                HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
            }
#elif ANDROID
            try
            {
                if (_tts != null && _ttsReady)
                {
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                    {
                        var bundle = new Android.OS.Bundle();
                        _tts.Speak(encouragement, Android.Speech.Tts.QueueMode.Add, bundle, "encouragement");
                    }
                    else
                    {
                        // Fallback for very old devices
#pragma warning disable CS0618
                        _tts.Speak(encouragement, Android.Speech.Tts.QueueMode.Add, null);
#pragma warning restore CS0618
                    }
                }
                else if (_tts == null)
                {
                    HelpLabel.Text = "Text-to-speech service is not initialized.";
                }
                else if (!_ttsReady)
                {
                    HelpLabel.Text = "Text-to-speech not ready. Please try again in a moment.";
                }
            }
            catch (System.Exception ex)
            {
                HelpLabel.Text = $"Text-to-speech failed: {ex.Message}";
            }
#else
            HelpLabel.Text = "Text-to-speech is not supported on this platform.";
#endif
        }

        private void TimeCheckModeButton_Clicked(object sender, EventArgs e)
        {
            _currentMode = Mode.TimeCheck;
            UpdateModeDisplay();
            HelpLabel.Text = "Mode switched to Time Check - announces time every 5 minutes (3 times).";
        }

        private void EncouragementModeButton_Clicked(object sender, EventArgs e)
        {
            _currentMode = Mode.Cycling;
            UpdateModeDisplay();
            HelpLabel.Text = "Mode switched to Cycling Encouragement - motivational messages at random intervals.";
        }

        private void UpdateModeDisplay()
        {
            if (_currentMode == Mode.TimeCheck)
            {
                TimeCheckModeButton.BackgroundColor = Colors.LightGreen;
                EncouragementModeButton.BackgroundColor = Colors.LightGray;
                CurrentModeLabel.Text = "Current Mode: Time Check (announces time every 5 minutes)";
                StartListeningButton.Text = "Speak Time";
            }
            else // Cycling
            {
                TimeCheckModeButton.BackgroundColor = Colors.LightGray;
                EncouragementModeButton.BackgroundColor = Colors.LightBlue;
                CurrentModeLabel.Text = "Current Mode: Cycling Encouragement (motivational messages at random intervals)";
                StartListeningButton.Text = "Speak Encouragement";
            }
        }

        private async void SaveCompanionSettings_Clicked(object sender, EventArgs e)
        {
            _settingsService.AssistantBaseUrl = AssistantUrlEntry.Text?.Trim() ?? string.Empty;
            _settingsService.DeviceToken = DeviceTokenEntry.Text?.Trim() ?? string.Empty;
            _settingsService.DeviceName = DeviceNameEntry.Text?.Trim() ?? string.Empty;
            _settingsService.Save();

            AssistantStatusLabel.Text = "Companion settings saved.";
            await DisplayAlert("Settings", "Companion settings saved.", "OK");
        }

        private void SendTestCommand_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            var intent = new Android.Content.Intent(
                Platform.CurrentActivity,
                typeof(TimeCheck.Platforms.Android.LocalSpeechCaptureActivity));
            Platform.CurrentActivity?.StartActivity(intent);
            AssistantStatusLabel.Text = "Local speech capture launched.";
#else
            AssistantStatusLabel.Text = "Local speech capture is only available on Android.";
#endif
        }

        private void SpeakCompanionCommand_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            var intent = new Android.Content.Intent(
                Platform.CurrentActivity,
                typeof(TimeCheck.Platforms.Android.SpeechCaptureActivity));
            Platform.CurrentActivity?.StartActivity(intent);
            AssistantStatusLabel.Text = "Speech capture launched.";
#else
            AssistantStatusLabel.Text = "Speech capture is only available on Android.";
#endif
        }

        private bool _companionServiceRunning = false;

        private async void ToggleCompanionService_Clicked(object sender, EventArgs e)
        {
            if (!_companionServiceRunning)
                await StartCompanionServiceAsync();
            else
                StopCompanionService();
        }

        private async Task StartCompanionServiceAsync()
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    AssistantStatusLabel.Text = "Microphone permission denied — cannot start service.";
                    return;
                }
            }

            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(TimeCheck.Platforms.Android.CommandForegroundService));
            intent.SetAction(TimeCheck.Platforms.Android.CommandForegroundService.ActionStart);
            context.StartForegroundService(intent);
            _companionServiceRunning = true;
            ToggleCompanionServiceButton.Text = "Stop Companion Service";
            ToggleCompanionServiceButton.BackgroundColor = Colors.DarkRed;
            SendTestCommandButton.IsEnabled = true;
            SpeakCompanionButton.IsEnabled = true;
            AssistantStatusLabel.Text = "Companion service running. Ready for voice commands.";
#else
            AssistantStatusLabel.Text = "Companion service is only available on Android.";
            await Task.CompletedTask;
#endif
        }

        private void StopCompanionService()
        {
#if ANDROID
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(TimeCheck.Platforms.Android.CommandForegroundService));
            intent.SetAction(TimeCheck.Platforms.Android.CommandForegroundService.ActionStop);
            context.StartService(intent);
#endif
            _companionServiceRunning = false;
            ToggleCompanionServiceButton.Text = "Start Companion Service";
            ToggleCompanionServiceButton.BackgroundColor = Colors.MediumPurple;
            SendTestCommandButton.IsEnabled = false;
            SpeakCompanionButton.IsEnabled = false;
            AssistantStatusLabel.Text = "Companion service stopped.";
        }

#if ANDROID
        private class TtsInitListener : Java.Lang.Object, Android.Speech.Tts.TextToSpeech.IOnInitListener
        {
            private readonly MainPage _page;
            public TtsInitListener(MainPage page) { _page = page; }
            public void OnInit([GeneratedEnum] Android.Speech.Tts.OperationResult status)
            {
                _page._ttsReady = (status == Android.Speech.Tts.OperationResult.Success);
            }
        }
#endif

        private void ToggleSettings_Clicked(object sender, EventArgs e)
        {
            var isVisible = !CompanionSettingsBody.IsVisible;
            CompanionSettingsBody.IsVisible = isVisible;
            ToggleSettingsButton.Text = isVisible ? "▲ Hide" : "▼ Show";
        }

        private void ToggleDeviceToken_Clicked(object sender, EventArgs e)
        {
            // Toggle the masking state for the device token entry and update button text
            DeviceTokenEntry.IsPassword = !DeviceTokenEntry.IsPassword;
            ToggleDeviceTokenButton.Text = DeviceTokenEntry.IsPassword ? "Show" : "Hide";
        }

        private void MinimizeAppButton_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity != null)
            {
                activity.MoveTaskToBack(true);
            }
            else
            {
                HelpLabel.Text = "Unable to minimize right now.";
            }
#elif WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
            // Minimize is not supported in MAUI Windows at this time
            HelpLabel.Text = "Minimize is not supported on Windows.";
#else
            HelpLabel.Text = "Minimize is not supported on this platform.";
#endif
        }

        private void CloseAppButton_Clicked(object sender, EventArgs e)
        {
#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
            // Forcefully terminate the process for a true app exit
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#elif ANDROID
            // Forcefully terminate the app process
            Java.Lang.JavaSystem.Exit(0);
#else
            HelpLabel.Text = "Close App is not supported on this platform.";
#endif
        }
    }
}