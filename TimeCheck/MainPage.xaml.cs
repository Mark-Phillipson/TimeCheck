// MainPage.xaml.cs
using Microsoft.Maui.Controls;
using System;
using TimeCheck.Services;
using Microsoft.Maui.ApplicationModel;

namespace TimeCheck
{
    public partial class MainPage : ContentPage
    {
        private readonly SpeechService _speechService;

        public MainPage(SpeechService speechService)
        {
            InitializeComponent();
            _speechService = speechService;
            _speechService.OnRecognized += UpdateTimeLabel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _speechService.InitializeAsync();
            try 
            {
                await _speechService.StartContinuousRecognitionAsync();
                HelpLabel.Text = "Listening... Say 'what is the time'";
            }
            catch (Exception ex)
            {
                HelpLabel.Text = "Failed to start speech recognition";
                Console.WriteLine($"Error starting speech recognition: {ex}");
            }
        }

        private void UpdateTimeLabel(string recognizedText)
        {
            Console.WriteLine($"Processing recognized text: {recognizedText}");
            if (recognizedText.Contains("time", StringComparison.OrdinalIgnoreCase))
            {
                var currentTime = DateTime.Now.ToString("h:mm");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimeLabel.Text = currentTime;
                    HelpLabel.Text = "Last updated: " + DateTime.Now.ToString("h:mm:ss tt");
                });
            }
        }
    }
}