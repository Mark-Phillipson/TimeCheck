using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public class EncouragementService : IEncouragementService, IAsyncDisposable
    {
        private readonly ITtsService _tts;
        private readonly ISettingsService _settings;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly Random _random = new Random();

        public EncouragementService(ITtsService tts, ISettingsService settings)
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
            try { _cts?.Cancel(); } catch { }
            _loopTask = null;
            return Task.CompletedTask;
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_settings.IsQuiet || !_settings.EncouragementEnabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                        continue;
                    }

                    var min = Math.Max(1, _settings.EncouragementIntervalMin);
                    var max = Math.Max(min, _settings.EncouragementIntervalMax);
                    var minutes = _random.Next(min, max + 1);
                    await Task.Delay(TimeSpan.FromMinutes(minutes), ct);

                    if (!_settings.IsQuiet && _settings.EncouragementEnabled)
                    {
                        var list = _settings.EncouragementMessages ?? new List<string>() { "Keep going!" };
                        var msg = list.Count > 0 ? list[_random.Next(list.Count)] : "Keep going!";
                        await _tts.SpeakAsync(msg);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* ignore transient */ }
            }
        }

        public ValueTask SayEncouragementNowAsync()
        {
            var list = _settings.EncouragementMessages ?? new List<string>() { "Keep going!" };
            var msg = list.Count > 0 ? list[_random.Next(list.Count)] : "Keep going!";
            return _tts.SpeakAsync(msg);
        }

        public async ValueTask DisposeAsync()
        {
            try { _cts?.Cancel(); } catch { }
            if (_loopTask != null) { try { await _loopTask; } catch { } }
        }
    }
}
