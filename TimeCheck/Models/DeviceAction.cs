namespace TimeCheck.Models;

public class DeviceAction
{
    public string Type { get; set; } = string.Empty;
    public IDictionary<string, object>? Params { get; set; }
}
