#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace TimeCheck;

public sealed class WindowsMainPage : ContentPage
{
    private enum Mode
    {
        TimeCheck,
        Cycling,
    }

    private readonly Label _helpLabel;
    private readonly Label _timeLabel;
    private readonly Label _modeLabel;
    private readonly Button _timeModeButton;
    private readonly Button _cyclingModeButton;
    private readonly Button _speakButton;
    private readonly Random _random = new();
    private readonly List<string> _encouragements = new()
    {
        "Keep pushing. The hill only wins if you stop.",
        "Stay on it. Smooth turns beat dramatic suffering.",
        "You are still moving, which means you are still winning.",
        "A hard climb is still progress. Keep pedaling.",
        "One more minute of effort changes the whole ride.",
        "Hold your line, keep your cadence, and get up the road.",
        "This is the part that makes the easy miles possible.",
        "You do not need easy. You need steady.",
    };

    private Mode _currentMode = Mode.TimeCheck;
    private bool _timersStarted;

    public WindowsMainPage()
    {
        Title = "TimeCheck";

        _helpLabel = new Label
        {
            Text = "Tap the button to hear the current time or an encouragement.",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 18,
        };

        _timeLabel = new Label
        {
            Text = "00:00",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 96,
        };

        _modeLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = 14,
            TextColor = Colors.Gray,
        };

        _timeModeButton = new Button
        {
            Text = "Time Check Mode",
        };
        _timeModeButton.Clicked += (_, _) =>
        {
            _currentMode = Mode.TimeCheck;
            UpdateModeDisplay();
            _helpLabel.Text = "Mode switched to Time Check.";
        };

        _cyclingModeButton = new Button
        {
            Text = "Beast Me",
        };
        _cyclingModeButton.Clicked += (_, _) =>
        {
            _currentMode = Mode.Cycling;
            UpdateModeDisplay();
            _helpLabel.Text = "Mode switched to Cycling Encouragement.";
        };

        _speakButton = new Button
        {
            Text = "Speak Time",
            FontSize = 28,
        };
        _speakButton.Clicked += (_, _) =>
        {
            if (_currentMode == Mode.TimeCheck)
            {
                SayTime();
            }
            else
            {
                SayEncouragement();
            }
        };

        var modeButtons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 10,
        };
        modeButtons.Add(_timeModeButton);
        modeButtons.Add(_cyclingModeButton, 1, 0);

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 30),
                Spacing = 16,
                Children =
                {
                    _helpLabel,
                    _timeLabel,
                    modeButtons,
                    _modeLabel,
                    _speakButton,
                },
            },
        };

        UpdateModeDisplay();
        UpdateTimeLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_timersStarted)
        {
            return;
        }

        _timersStarted = true;

        Dispatcher.StartTimer(TimeSpan.FromMinutes(1), () =>
        {
            UpdateTimeLabel();
            return true;
        });

        Dispatcher.StartTimer(TimeSpan.FromMinutes(5), () =>
        {
            if (_currentMode == Mode.TimeCheck)
            {
                SayTime();
            }

            return true;
        });

        ScheduleNextEncouragement();
    }

    private void UpdateModeDisplay()
    {
        if (_currentMode == Mode.TimeCheck)
        {
            _timeModeButton.BackgroundColor = Colors.LightGreen;
            _cyclingModeButton.BackgroundColor = Colors.LightGray;
            _modeLabel.Text = "Current Mode: Time Check (every 5 minutes)";
            _speakButton.Text = "Speak Time";
        }
        else
        {
            _timeModeButton.BackgroundColor = Colors.LightGray;
            _cyclingModeButton.BackgroundColor = Colors.LightBlue;
            _modeLabel.Text = "Current Mode: Cycling Encouragement (random interval)";
            _speakButton.Text = "Speak Encouragement";
        }
    }

    private void UpdateTimeLabel()
    {
        _timeLabel.Text = DateTime.Now.ToString("hh:mm");
    }

    private void ScheduleNextEncouragement()
    {
        var delay = TimeSpan.FromMinutes(_random.NextDouble() * 9 + 1);
        Dispatcher.StartTimer(delay, () =>
        {
            if (_currentMode == Mode.Cycling)
            {
                SayEncouragement();
            }

            ScheduleNextEncouragement();
            return false;
        });
    }

    private void SayTime()
    {
        var currentTime = DateTime.Now.ToString("h:mm tt");
        SpeakAsync($"The time is {currentTime}");
    }

    private void SayEncouragement()
    {
        var message = _encouragements[_random.Next(_encouragements.Count)];
        SpeakAsync(message);
    }

    private void SpeakAsync(string text)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                var stream = await synthesizer.SynthesizeTextToStreamAsync(text);
                var mediaPlayer = new Windows.Media.Playback.MediaPlayer();
                mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType);
                mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                _helpLabel.Text = $"Text-to-speech failed: {ex.Message}";
            }
        });
    }
}
#endif