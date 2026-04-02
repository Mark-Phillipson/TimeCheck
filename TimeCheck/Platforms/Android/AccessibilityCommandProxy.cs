using TimeCheck.Services;

namespace TimeCheck.Platforms.Android;

/// <summary>
/// DI-registered proxy that delegates to the live <see cref="CompanionAccessibilityService"/> instance.
/// Returns false (not connected) when the accessibility service has not been enabled by the user.
/// </summary>
public sealed class AccessibilityCommandProxy : IAccessibilityCommandService
{
    public bool IsConnected => CompanionAccessibilityService.Instance != null;

    public Task<bool> PerformNavigationAsync(string action) =>
        CompanionAccessibilityService.Instance?.PerformNavigationAsync(action)
        ?? Task.FromResult(false);

    public Task<bool> PerformScrollAsync(string direction) =>
        CompanionAccessibilityService.Instance?.PerformScrollAsync(direction)
        ?? Task.FromResult(false);
}
