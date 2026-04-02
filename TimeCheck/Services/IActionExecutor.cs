using TimeCheck.Models;

namespace TimeCheck.Services;

public interface IActionExecutor
{
    Task<ActionExecutionResult> ExecuteAsync(DeviceAction action, CancellationToken cancellationToken = default);
}
