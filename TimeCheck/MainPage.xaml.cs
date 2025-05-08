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
using System.Timers;

namespace TimeCheck
{
    public partial class MainPage : ContentPage
    {
#if ANDROID
        private Android.Speech.Tts.TextToSpeech? _tts;
        private bool _ttsReady = false;
#endif

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            HelpLabel.Text = "Tap 'Speak Time' to hear the time.";
            StartMinuteTimer();
#if ANDROID
            if (_tts == null)
            {
                _tts = new Android.Speech.Tts.TextToSpeech(Platform.CurrentActivity, new TtsInitListener(this));
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
            var currentTime = DateTime.Now.ToString("h:mm");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimeLabel.Text = currentTime;
                HelpLabel.Text = "Last updated: " + DateTime.Now.ToString("h:mm:ss tt");
            });
        }

        private void StartMinuteTimer()
        {
            // Update every minute for the clock display
            Dispatcher.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                UpdateTimeLabel();
                return true; // Repeat every minute
            });
            // Also update immediately on load
            UpdateTimeLabel();

            // New: Say the time every 5 minutes
            Dispatcher.StartTimer(TimeSpan.FromMinutes(5), () =>
            {
                SayTime();
                return true; // Repeat every 5 minutes
            });
        }

        private void SayTime()
        {
            UpdateTimeLabel();
#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
            try
            {
                var currentTime = DateTime.Now.ToString("h:mm tt on MMMM dd");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                        var stream = await synthesizer.SynthesizeTextToStreamAsync($"The current time is {currentTime}");
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
                    var currentTime = DateTime.Now.ToString("h:mm tt on MMMM dd");
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                    {
#pragma warning disable CS0618
                        _tts.Speak($"The current time is {currentTime}", Android.Speech.Tts.QueueMode.Flush, null, "utteranceId");
#pragma warning restore CS0618
                    }
                    else
                    {
                        _tts.Speak($"The current time is {currentTime}", Android.Speech.Tts.QueueMode.Flush, null);
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

        private void StartListeningButton_Clicked(object sender, EventArgs e)
        {
            SayTime();
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

        private void MinimizeAppButton_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            Platform.CurrentActivity.MoveTaskToBack(true);
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