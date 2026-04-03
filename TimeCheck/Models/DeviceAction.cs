namespace TimeCheck.Models;

public class DeviceAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string>? Params { get; set; }
}
