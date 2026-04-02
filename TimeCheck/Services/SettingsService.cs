using Microsoft.Maui.Storage;

namespace TimeCheck.Services;

public class SettingsService : ISettingsService
{
    const string AssistantUrlKey = "AssistantBaseUrl";
    const string DeviceTokenKey = "DeviceToken";
    const string DeviceNameKey = "DeviceName";

    public string AssistantBaseUrl { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;

    public void Load()
    {
        AssistantBaseUrl = Preferences.Get(AssistantUrlKey, string.Empty);
        DeviceToken = Preferences.Get(DeviceTokenKey, string.Empty);
        DeviceName = Preferences.Get(DeviceNameKey, string.Empty);
    }

    public void Save()
    {
        Preferences.Set(AssistantUrlKey, AssistantBaseUrl);
        Preferences.Set(DeviceTokenKey, DeviceToken);
        Preferences.Set(DeviceNameKey, DeviceName);
    }
}
