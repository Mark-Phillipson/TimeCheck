using Microsoft.Maui.Controls;
using System;

namespace TimeCheck;

public partial class TestStartupPage : ContentPage
{
    public TestStartupPage()
    {
        InitializeComponent();
        PlatformLabel.Text = $"Platform: {DeviceInfo.Current.Platform}";
        TimeLabel.Text = DateTime.Now.ToString("F");
    }
}
