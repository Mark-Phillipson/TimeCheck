using TimeCheck.Models;

namespace TimeCheck.Services;

public interface IAssistantApiClient
{
    Task<CommandResponse> SendCommandAsync(CommandRequest request, CancellationToken cancellationToken = default);
}
