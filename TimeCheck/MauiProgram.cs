using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeCheck.Shared.Services;
using TimeCheck.Services;
using System.Reflection;
#if ANDROID
using TimeCheck.Platforms.Android;
#endif

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

        // Register MainPage
        builder.Services.AddTransient<MainPage>();

        // Companion configuration and APIs
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IAssistantApiClient, AssistantApiClient>();

        // Add device-specific services used by the TimeCheck.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

#if ANDROID
        // Android companion services
        builder.Services.AddSingleton<IAccessibilityCommandService, AccessibilityCommandProxy>();
        builder.Services.AddSingleton<IActionExecutor, AndroidActionExecutor>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
