using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeCheck.Shared.Services;
using TimeCheck.Services;
using System.Reflection;

namespace TimeCheck;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        try
        {
            // Create configuration from embedded resource
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("TimeCheck.appsettings.json");

            if (stream != null)
            {
                builder.Configuration.AddJsonStream(stream);
                
                // Register AppSettings
                var appSettings = new AppSettings
                {
                    AzureSpeechApiKey = builder.Configuration["AzureSpeechApiKey"] ?? "387fcb951139493aac991c514fb89496",
                    AzureRegion = builder.Configuration["AzureRegion"] ?? "uksouth"
                };
                builder.Services.AddSingleton(appSettings);

                // Register SpeechService
                builder.Services.AddSingleton<SpeechService>(sp => 
                    new SpeechService(appSettings.AzureSpeechApiKey));
            }
            else
            {
                // Fallback to hardcoded values if config file cannot be loaded
                var appSettings = new AppSettings
                {
                    AzureSpeechApiKey = "387fcb951139493aac991c514fb89496",
                    AzureRegion = "uksouth"
                };
                builder.Services.AddSingleton(appSettings);
                builder.Services.AddSingleton<SpeechService>(sp => 
                    new SpeechService(appSettings.AzureSpeechApiKey));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex}");
            // Fallback to hardcoded values
            var appSettings = new AppSettings
            {
                AzureSpeechApiKey = "387fcb951139493aac991c514fb89496",
                AzureRegion = "uksouth"
            };
            builder.Services.AddSingleton(appSettings);
            builder.Services.AddSingleton<SpeechService>(sp => 
                new SpeechService(appSettings.AzureSpeechApiKey));
        }

        // Register MainPage
        builder.Services.AddTransient<MainPage>();

        // Add device-specific services used by the TimeCheck.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
