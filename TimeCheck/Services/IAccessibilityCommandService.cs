namespace TimeCheck.Services;

public interface IAccessibilityCommandService
{
    bool IsConnected { get; }
    Task<bool> PerformNavigationAsync(string action);
    Task<bool> PerformScrollAsync(string direction);
}
