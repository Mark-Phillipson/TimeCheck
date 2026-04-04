namespace TimeCheck.Services;

public interface IAccessibilityCommandService
{
    bool IsConnected { get; }
    Task<bool> PerformNavigationAsync(string action);
    Task<bool> PerformScrollAsync(string direction);
    // Indicates whether this implementation can detect other accessibility tools (e.g., Voice Access)
    bool CanDetectVoiceAccess { get; }
    // Returns true when Voice Access appears enabled on the device (heuristic detection).
    Task<bool> IsVoiceAccessEnabledAsync();
}
