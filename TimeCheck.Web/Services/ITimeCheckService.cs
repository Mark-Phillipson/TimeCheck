using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public interface ITimeCheckService
    {
        Task StartAsync();
        Task StopAsync();
        ValueTask SayTimeNowAsync();
    }
}
