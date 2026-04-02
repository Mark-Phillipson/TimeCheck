using System.Net.Http.Json;
using TimeCheck.Models;

namespace TimeCheck.Services;

public class AssistantApiClient : IAssistantApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settings;

    public AssistantApiClient(HttpClient httpClient, ISettingsService settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<CommandResponse> SendCommandAsync(CommandRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AssistantBaseUrl))
        {
            throw new InvalidOperationException("Assistant base URL is not configured.");
        }

        var url = new Uri(new Uri(_settings.AssistantBaseUrl.TrimEnd('/')), "api/command");

        request.DeviceToken ??= _settings.DeviceToken;
        request.DeviceName ??= _settings.DeviceName;

        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandResponse>(cancellationToken: cancellationToken);

        return result ?? new CommandResponse { TextResponse = "Empty assistant response" };
    }
}
