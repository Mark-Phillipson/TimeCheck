namespace TimeCheck.Services;

public interface ICommandCaptureService
{
    /// <summary>Triggers speech recognition and returns the transcribed text, or null if cancelled/unavailable.</summary>
    Task<string?> CaptureCommandAsync(CancellationToken cancellationToken = default);
}
