// Services/SpeechService.cs
using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
#if ANDROID
using Android.Runtime;
using Android.Speech;
using Android.OS;
using Android.Content;
using Microsoft.Maui.ApplicationModel;
#endif
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace TimeCheck.Services
{
    public class SpeechService
    {
#if ANDROID
        private Android.Speech.SpeechRecognizer? _recognizer;
        private Android.Speech.Tts.TextToSpeech? _tts;
#elif WINDOWS
        private Windows.Media.SpeechRecognition.SpeechRecognizer? _recognizer;
        private Windows.Media.SpeechSynthesis.SpeechSynthesizer? _synthesizer;
#endif
        private bool _isInitialized;

        public event Action<string>? OnRecognized;

        public SpeechService()
        {
#if ANDROID
            _tts = new Android.Speech.Tts.TextToSpeech(Platform.CurrentActivity, new TTSListener());
#elif WINDOWS
            _synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
#endif
        }

        public async Task InitializeAsync()
        {
            try
            {
#if ANDROID
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    System.Console.WriteLine("Microphone permission not granted");
                    return;
                }

                _recognizer = Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(Platform.CurrentActivity);
                if (_recognizer == null)
                {
                    System.Console.WriteLine("Could not create speech recognizer");
                    return;
                }

                _recognizer.SetRecognitionListener(new SpeechRecognitionListener
                {
                    OnRecognizedAction = (text) => 
                    {
                        if (text?.Contains("time", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            SpeakCurrentTime();
                        }
                        if (text != null)
                        {
                            OnRecognized?.Invoke(text);
                        }
                    }
                });
#elif WINDOWS
                var microphonePermission = await Windows.Security.Authorization.AppCapabilityAccess.AppCapability.Create("microphone").RequestAccessAsync();
                if (microphonePermission != Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus.Allowed)
                {
                    System.Console.WriteLine("Microphone permission not granted");
                    return;
                }

                _recognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();
                _recognizer.ContinuousRecognitionSession.ResultGenerated += (sender, args) =>
                {
                    if (args.Result.Text.Contains("time", StringComparison.OrdinalIgnoreCase))
                    {
                        SpeakCurrentTime();
                    }
                    OnRecognized?.Invoke(args.Result.Text);
                };
#endif
                _isInitialized = true;
                Console.WriteLine("Speech recognizer initialized successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error initializing speech service: {ex}");
            }
        }

        public async Task StartContinuousRecognitionAsync()
        {
            if (!_isInitialized)
            {
                System.Console.WriteLine("Speech service not initialized");
                return;
            }

            try
            {
#if ANDROID
                var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                _recognizer?.StartListening(intent);
#elif WINDOWS
                if (_recognizer?.ContinuousRecognitionSession != null)
                {
                    await _recognizer.ContinuousRecognitionSession.StartAsync();
                }
#endif
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error starting recognition: {ex}");
            }
        }

        private void SpeakCurrentTime()
        {
            var currentTime = DateTime.Now.ToString("h:mm tt on MMMM dd");
#if ANDROID
            if (_tts != null)
            {
                _tts.Speak($"The current time is {currentTime}", Android.Speech.Tts.QueueMode.Flush, null);
            }
#elif WINDOWS
            if (_synthesizer != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var stream = await _synthesizer.SynthesizeTextToStreamAsync($"The current time is {currentTime}");
                        var mediaPlayer = new Windows.Media.Playback.MediaPlayer();
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(stream, stream.ContentType);
                        mediaPlayer.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error speaking time: {ex}");
                    }
                });
            }
#endif
        }
    }

#if ANDROID
    public class TTSListener : Java.Lang.Object, Android.Speech.Tts.TextToSpeech.IOnInitListener
    {
        public void OnInit(Android.Speech.Tts.OperationResult status)
        {
            if (status.Equals(Android.Speech.Tts.OperationResult.Success))
            {
                Console.WriteLine("TTS initialized successfully");
            }
            else
            {
                Console.WriteLine("Failed to initialize TTS");
            }
        }
    }

    public class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
    {
        public Action<string>? OnRecognizedAction { get; set; }

        public void OnBeginningOfSpeech() { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEndOfSpeech() { }
        public void OnError([GeneratedEnum] SpeechRecognizerError error) 
        {
            Console.WriteLine($"Speech recognition error: {error}");
        }
        public void OnEvent(int eventType, Bundle? @params) { }
        public void OnPartialResults(Bundle? partialResults) { }
        public void OnReadyForSpeech(Bundle? @params) { }
        public void OnRmsChanged(float rmsdB) { }

        public void OnResults(Bundle? results)
        {
            var matches = results?.GetStringArrayList(Android.Speech.SpeechRecognizer.ResultsRecognition);
            if (matches?.Count > 0)
            {
                var text = matches[0];
                OnRecognizedAction?.Invoke(text);
            }
        }
    }
#endif
}