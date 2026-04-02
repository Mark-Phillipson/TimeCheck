#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace TimeCheck;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        // Do not set Application.MainPage here. Create the Window in CreateWindow.
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if WINDOWS || WINDOWS10_0_17763_0 || WINDOWS10_0_19041_0
        var window = new Window(new WindowsMainPage())
        {
            Title = "TimeCheck",
            Width = 900,
            Height = 700,
        };

        window.Created += (_, _) =>
        {
            if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                var hWnd = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                appWindow?.MoveAndResize(new Windows.Graphics.RectInt32(120, 120, 900, 700));
                nativeWindow.Activate();
            }
        };

        return window;
#else
#if ANDROID
    // Create the window with a DI-resolved MainPage (avoids setting Application.MainPage early)
    return new Window(_services.GetRequiredService<MainPage>());
#else
    return new Window(_services.GetRequiredService<MainPage>());
#endif
#endif
    }
}
