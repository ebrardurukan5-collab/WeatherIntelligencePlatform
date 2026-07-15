using WeatherIntelligencePlatform.DTOs;

namespace WeatherIntelligencePlatform.Services.Providers;

public interface IWeatherProvider
{
    string ProviderName { get; }
    Task<NormalizedWeatherResult> GetWeatherAsync(string city);
}