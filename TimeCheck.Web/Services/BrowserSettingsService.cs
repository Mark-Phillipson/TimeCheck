using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public class BrowserSettingsService : ISettingsService
    {
        private const string StorageKey = "timecheck.settings.v1";
        private readonly IJSRuntime _js;

        public bool IsQuiet { get; set; } = false;
        public int TimeCheckIntervalMinutes { get; set; } = 5;
        public int EncouragementIntervalMin { get; set; } = 10;
        public int EncouragementIntervalMax { get; set; } = 20;
        public bool EncouragementEnabled { get; set; } = true;
        public List<string> EncouragementMessages { get; set; } = new List<string>
        {
            "Keep going — you're doing great!",
            "Push a little harder — you got this!",
            "Almost there — keep the cadence!",
            "Nice work — stay with it!"
        };

        public BrowserSettingsService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task LoadAsync()
        {
            try
            {
                var json = await _js.InvokeAsync<string>("timecheck.storageGet", StorageKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var dto = JsonSerializer.Deserialize<BrowserSettingsDto>(json);
                    if (dto != null)
                    {
                        IsQuiet = dto.IsQuiet;
                        TimeCheckIntervalMinutes = dto.TimeCheckIntervalMinutes > 0 ? dto.TimeCheckIntervalMinutes : TimeCheckIntervalMinutes;
                        EncouragementIntervalMin = dto.EncouragementIntervalMin > 0 ? dto.EncouragementIntervalMin : EncouragementIntervalMin;
                        EncouragementIntervalMax = dto.EncouragementIntervalMax > 0 ? dto.EncouragementIntervalMax : EncouragementIntervalMax;
                        EncouragementEnabled = dto.EncouragementEnabled;
                        EncouragementMessages = dto.EncouragementMessages ?? EncouragementMessages;
                    }
                }
            }
            catch
            {
                // ignore and use defaults
            }
        }

        public async Task SaveAsync()
        {
            var dto = new BrowserSettingsDto
            {
                IsQuiet = IsQuiet,
                TimeCheckIntervalMinutes = TimeCheckIntervalMinutes,
                EncouragementIntervalMin = EncouragementIntervalMin,
                EncouragementIntervalMax = EncouragementIntervalMax,
                EncouragementEnabled = EncouragementEnabled,
                EncouragementMessages = EncouragementMessages
            };

            var json = JsonSerializer.Serialize(dto);
            await _js.InvokeVoidAsync("timecheck.storageSet", StorageKey, json);
        }

        private class BrowserSettingsDto
        {
            public bool IsQuiet { get; set; }
            public int TimeCheckIntervalMinutes { get; set; }
            public int EncouragementIntervalMin { get; set; }
            public int EncouragementIntervalMax { get; set; }
            public bool EncouragementEnabled { get; set; }
            public List<string>? EncouragementMessages { get; set; }
        }
    }
}
