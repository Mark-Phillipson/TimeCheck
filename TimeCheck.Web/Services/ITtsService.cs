using System.Threading.Tasks;
using System;

namespace TimeCheck.Web.Services
{
    public interface ITtsService
    {
        ValueTask SpeakAsync(string text, bool cancelPrior = true);
        ValueTask CancelAsync();
        ValueTask<bool> IsSupportedAsync();
    }
}
