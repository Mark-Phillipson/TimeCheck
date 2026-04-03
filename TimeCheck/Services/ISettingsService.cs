namespace TimeCheck.Services;

public interface ISettingsService
{
    string AssistantBaseUrl { get; set; }
    string DeviceToken { get; set; }
    string DeviceName { get; set; }
    bool IsQuiet { get; set; }

    void Load();
    void Save();
}
