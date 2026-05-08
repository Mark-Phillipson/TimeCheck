using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public class TimeCheckService : ITimeCheckService, IAsyncDisposable
    {
        private readonly ITtsService _tts;
        private readonly ISettingsService _settings;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public TimeCheckService(ITtsService tts, ISettingsService settings)
        {
            _tts = tts;
            _settings = settings;
        }

        public Task StartAsync()
        {
            if (_loopTask != null && !_loopTask.IsCompleted)
                return Task.CompletedTask;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => LoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { }
            _loopTask = null;
            return Task.CompletedTask;
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            // Immediate first-run alignment: speak next at interval boundary
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_settings.IsQuiet)
                    {
                        var now = DateTime.Now;
                        var currentTime = now.ToString("h:mm tt", CultureInfo.InvariantCulture);
                        await _tts.SpeakAsync($"The time is {currentTime}");
                    }

                    var delayMinutes = Math.Max(1, _settings.TimeCheckIntervalMinutes);
                    await Task.Delay(TimeSpan.FromMinutes(delayMinutes), ct);
                }
                catch (OperationCanceledException) { break; }
                catch { /* swallow transient errors */ }
            }
        }

        public ValueTask SayTimeNowAsync()
        {
            var now = DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture);
            return _tts.SpeakAsync($"The time is {now}");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { }
            if (_loopTask != null)
            {
                try { await _loopTask; } catch { }
            }
        }
    }
}
