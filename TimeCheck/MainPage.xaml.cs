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
{    public partial class MainPage : ContentPage
    {
#if ANDROID
        private Android.Speech.Tts.TextToSpeech? _tts;
        private bool _ttsReady = false;
#endif        // Mode management
        private bool _isTimeCheckMode = true; // Start with time check mode
        private readonly List<string> _cyclingEncouragements = new List<string>
        {
            // General Motivation
            "Keep pedaling! Every hill makes you stronger!",
            "You're doing great! Hills are just strength training in disguise!",
            "Push through! The view from the top is worth it!",
            "One pedal at a time! You've got this uphill climb!",
            "Hills build character and strong legs! Keep going!",
            "Remember, what goes up must come down - enjoy the descent!",
            "You're conquering this hill like a champion cyclist!",
            "Every uphill battle makes the flat roads feel like flying!",
            "Hills are where legends are made! You're becoming one!",
            "The steeper the hill, the stronger you become!",
            
            // Hill Climbing Specific
            "Embrace the burn! Your legs are getting stronger with every pedal stroke!",
            "This hill is temporary, but your strength is permanent!",
            "Channel your inner mountain goat - you were born to climb!",
            "Every hill is a chance to prove your cycling prowess!",
            "Hills don't get easier, you just get stronger!",
            "Conquer this incline like the cycling warrior you are!",
            "Your bike is your steed, and you're climbing to victory!",
            "Each pedal stroke is writing your cycling success story!",
            "Hills are nature's way of making you a better cyclist!",
            "Rise above the challenge - literally and figuratively!",
            
            // Mental Strength
            "Your mind is stronger than this hill - prove it!",
            "Pain is temporary, but the satisfaction of conquering this hill is forever!",
            "You've climbed harder hills than this - keep that momentum!",
            "Transform this struggle into strength!",
            "Every difficult climb makes you mentally tougher!",
            "Your determination is more powerful than any incline!",
            "This hill is testing your character - show it what you're made of!",
            "Mental fortitude is your secret cycling weapon!",
            "You're not just climbing a hill, you're building resilience!",
            "Turn this challenge into your cycling triumph!",
            
            // Technique and Form
            "Steady rhythm, controlled breathing - you've got this technique down!",
            "Keep your cadence smooth and let your fitness carry you!",
            "Perfect your climbing form with every pedal revolution!",
            "Stay seated and maintain that powerful pedal stroke!",
            "Your technique is improving with every challenging climb!",
            "Focus on efficiency - smooth, powerful, relentless!",
            "Keep those shoulders relaxed and legs driving forward!",
            "You're dancing on the pedals like a true cyclist!",
            "Maintain that beautiful cycling rhythm up this incline!",
            "Your form is poetry in motion on two wheels!",
            
            // Achievement and Progress
            "Every meter climbed is a victory worth celebrating!",
            "You're setting new personal records with every ride!",
            "This climb is making you the cyclist you've always wanted to be!",
            "Your progress is visible in every confident pedal stroke!",
            "You're rewriting the definition of what's possible on a bike!",
            "Each hill conquered adds to your cycling legacy!",
            "Your improvement is inspiring - keep pushing those limits!",
            "You're becoming the cyclist others aspire to be!",
            "This climb is proof of how far you've come!",
            "Your dedication is transforming you into a climbing machine!",
            
            // Energy and Power
            "Feel that power flowing through your legs!",
            "You're an unstoppable cycling force of nature!",
            "Channel your inner cycling dynamo and power through!",
            "Your legs are pistons of pure cycling energy!",
            "Unleash the beast within - you're stronger than you know!",
            "Every breath fuels your incredible cycling machine!",
            "You're generating serious watts up this incline!",
            "Tap into that deep well of cycling strength!",
            "Your power output is absolutely phenomenal!",
            "Feel the surge of energy as you dominate this climb!",
            
            // Scenic and Inspirational
            "The best views come after the hardest climbs!",
            "You're earning every beautiful vista with each pedal stroke!",
            "This struggle is the price of admission to cycling paradise!",
            "The summit view will be worth every challenging meter!",
            "You're creating memories that will last a lifetime!",
            "This hill is your gateway to cycling enlightenment!",
            "The descent awaits - earn it with this magnificent climb!",
            "You're writing your own cycling adventure story!",
            "Every hill conquered adds another chapter to your cycling journey!",
            "This climb is preparing you for even greater cycling adventures!",
            
            // Comparative and Competitive
            "You're climbing stronger than yesterday's version of yourself!",
            "This hill doesn't stand a chance against your determination!",
            "You're outpacing doubt and self-limitation with every rotation!",
            "Your climbing ability is evolving with every challenging ride!",
            "You're racing against your former limits and winning!",
            "This incline is no match for your upgraded cycling skills!",
            "You're demolishing personal barriers one hill at a time!",
            "Your progress would make professional cyclists proud!",
            "You're climbing like someone who refuses to accept defeat!",
            "Your improvement trajectory is absolutely remarkable!",
            
            // Motivational Metaphors
            "You're a cycling phoenix rising above every challenge!",
            "This hill is just another stepping stone to cycling greatness!",
            "You're forging cycling steel in the fire of this incline!",
            "Like a cycling alchemist, you're turning struggle into strength!",
            "You're the architect of your own cycling success story!",
            "This climb is sculpting you into a cycling masterpiece!",
            "You're a cycling warrior conquering the battlefield of hills!",
            "Every pedal stroke is painting your cycling legacy!",
            "You're the captain of your cycling destiny!",
            "This hill is your cycling classroom, and you're acing the test!",
            
            // Encouragement for Persistence
            "Keep going - your breakthrough moment is just ahead!",
            "Persistence is your cycling superpower - use it now!",
            "You've never regretted finishing a challenging climb!",
            "Your future self will thank you for not giving up!",
            "Champions are made in moments exactly like this one!",
            "This is where ordinary cyclists become extraordinary!",
            "Your refusal to quit is what separates you from the rest!",
            "Every second you persist is building cycling character!",
            "You're proving that you have the heart of a true cyclist!",
            "Quitting is not in your cycling vocabulary!",
            
            // Final Push and Summit
            "You're so close to the top - dig deep and finish strong!",
            "The summit is calling your name - answer with power!",
            "These final meters will define your cycling character!",
            "You can almost taste the victory of reaching the top!",
            "Push through this last section like the champion you are!",
            "The finish line of this climb is within your grasp!",
            "You're meters away from another cycling triumph!",
            "This is your moment to shine as a cyclist!",
            "Cross that summit line with pride and power!",
            "You're about to add another conquered hill to your legacy!"
        };
        private readonly Random _random = new Random();

        public MainPage()
        {
            InitializeComponent();
            SizeChanged += MainPage_SizeChanged;
        }        private void MainPage_SizeChanged(object? sender, EventArgs e)
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
            TimeLabel.FontSize = isLandscape ? 70 : 120;
        }protected override void OnAppearing()
        {
            base.OnAppearing();
            HelpLabel.Text = "Tap button to hear time or encouragement based on current mode.";
            
            // Ensure toggle buttons are visible initially
            TimeCheckModeButton.IsVisible = true;
            EncouragementModeButton.IsVisible = true;
            CurrentModeLabel.IsVisible = true;
            
            UpdateModeDisplay(); // Initialize mode display
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
                if (_isTimeCheckMode)
                {
                    SayTime();
                }
                return true; // Repeat every 5 minutes
            });

            // Encouragement Mode: Say encouragement every 10 minutes (once)
            Dispatcher.StartTimer(TimeSpan.FromMinutes(10), () =>
            {
                if (!_isTimeCheckMode)
                {
                    SayEncouragement();
                }
                return true; // Repeat every 10 minutes
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
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                        {
#pragma warning disable CS0618
                            _tts.Speak($"The time is {currentTime}", Android.Speech.Tts.QueueMode.Add, null, $"utteranceId_{i}");
#pragma warning restore CS0618
                        }
                        else
                        {
                            _tts.Speak($"The time is {currentTime}", Android.Speech.Tts.QueueMode.Add, null);
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
            if (_isTimeCheckMode)
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
#pragma warning disable CS0618
                        _tts.Speak(encouragement, Android.Speech.Tts.QueueMode.Add, null, "encouragement");
#pragma warning restore CS0618
                    }
                    else
                    {
                        _tts.Speak(encouragement, Android.Speech.Tts.QueueMode.Add, null);
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
            _isTimeCheckMode = true;
            UpdateModeDisplay();
            HelpLabel.Text = "Mode switched to Time Check - announces time every 5 minutes (3 times).";
        }

        private void EncouragementModeButton_Clicked(object sender, EventArgs e)
        {
            _isTimeCheckMode = false;
            UpdateModeDisplay();
            HelpLabel.Text = "Mode switched to Cycling Encouragement - motivational messages every 10 minutes.";
        }

        private void UpdateModeDisplay()
        {
            if (_isTimeCheckMode)
            {
                TimeCheckModeButton.BackgroundColor = Colors.LightGreen;
                EncouragementModeButton.BackgroundColor = Colors.LightGray;
                CurrentModeLabel.Text = "Current Mode: Time Check (announces time every 5 minutes)";
                StartListeningButton.Text = "Speak Time";
            }
            else
            {
                TimeCheckModeButton.BackgroundColor = Colors.LightGray;
                EncouragementModeButton.BackgroundColor = Colors.LightBlue;
                CurrentModeLabel.Text = "Current Mode: Cycling Encouragement (motivational messages every 10 minutes)";
                StartListeningButton.Text = "Speak Encouragement";
            }
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