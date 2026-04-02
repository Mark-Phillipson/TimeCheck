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

        // Register MainPage
        builder.Services.AddTransient<MainPage>();

        // Companion configuration and APIs
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IAssistantApiClient, AssistantApiClient>();

        // Add device-specific services used by the TimeCheck.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
