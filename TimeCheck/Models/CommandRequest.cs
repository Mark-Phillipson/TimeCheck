namespace TimeCheck.Models;

public class CommandRequest
{
    public string Command { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
}
