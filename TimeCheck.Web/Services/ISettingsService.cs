using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public interface ISettingsService
    {
        bool IsQuiet { get; set; }
        int TimeCheckIntervalMinutes { get; set; }
        int EncouragementIntervalMin { get; set; }
        int EncouragementIntervalMax { get; set; }
        bool EncouragementEnabled { get; set; }
        List<string> EncouragementMessages { get; set; }

        Task LoadAsync();
        Task SaveAsync();
    }
}
