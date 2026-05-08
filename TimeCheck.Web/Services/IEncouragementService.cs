using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public interface IEncouragementService
    {
        Task StartAsync();
        Task StopAsync();
        ValueTask SayEncouragementNowAsync();
    }
}
