using Microsoft.Extensions.Configuration;

namespace TimeCheck.Services
{
    public class AppSettings
    {
        public string AzureSpeechApiKey { get; set; } = string.Empty;
        public string AzureRegion { get; set; } = "uksouth";
    }
}