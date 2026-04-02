namespace TimeCheck.Models;

public class CommandResponse
{
    public string TextResponse { get; set; } = string.Empty;
    public List<DeviceAction> Actions { get; set; } = new();
}
