using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace TimeCheck.Web.Services
{
    public class TtsService : ITtsService
    {
        private readonly IJSRuntime _js;

        public TtsService(IJSRuntime js)
        {
            _js = js;
        }

        public ValueTask SpeakAsync(string text, bool cancelPrior = true)
        {
            return _js.InvokeVoidAsync("timecheck.speak", text ?? string.Empty, cancelPrior);
        }

        public ValueTask CancelAsync()
        {
            return _js.InvokeVoidAsync("timecheck.cancel");
        }

        public async ValueTask<bool> IsSupportedAsync()
        {
            try
            {
                return await _js.InvokeAsync<bool>("timecheck.isSupported");
            }
            catch
            {
                return false;
            }
        }
    }
}
