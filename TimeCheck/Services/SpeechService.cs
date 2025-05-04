    // Services/SpeechService.cs
using Microsoft.CognitiveServices.Speech;
using System;
using System.Threading.Tasks;
#if ANDROID
using Android.Runtime;
using Microsoft.Maui.ApplicationModel;
#endif

namespace TimeCheck.Services
{
    public class SpeechService
    {
        private SpeechRecognizer? _recognizer;
        private SpeechConfig? _config;
#if ANDROID
        private Android.Speech.Tts.TextToSpeech? _tts;
#endif
        private bool _isInitialized;
        private readonly string _apiKey;

        public event Action<string>? OnRecognized;

        public SpeechService(string apiKey)
        {
            _apiKey = apiKey;
#if ANDROID
            _tts = new Android.Speech.Tts.TextToSpeech(Platform.CurrentActivity, new TTSListener());
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
#endif
                if (string.IsNullOrEmpty(_apiKey))
                {
                    System.Console.WriteLine("Azure Speech API key not provided");
                    return;
                }

                _config = SpeechConfig.FromSubscription(_apiKey, "uksouth");
                _recognizer = new SpeechRecognizer(_config);
                _recognizer.Recognized += Recognizer_Recognized;
                Console.WriteLine("Speech recognizer initialized successfully");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error initializing speech service: {ex}");
            }
        }

        public async Task StartContinuousRecognitionAsync()
        {
            if (!_isInitialized || _recognizer == null)
            {
                System.Console.WriteLine("Speech service not initialized");
                return;
            }

            try
            {
                await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error starting recognition: {ex}");
            }
        }

        private void Recognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"Recognized: {e.Result.Text}");
                OnRecognized?.Invoke(e.Result.Text);
                if (e.Result.Text.Contains(" time", StringComparison.OrdinalIgnoreCase))
                {
                    SpeakCurrentTime();
                }
            }
            else
            {
                Console.WriteLine("Speech not recognized.");
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
#endif
}