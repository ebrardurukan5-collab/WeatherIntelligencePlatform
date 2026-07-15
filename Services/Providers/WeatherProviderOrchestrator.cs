using WeatherIntelligencePlatform.DTOs;
using WeatherIntelligencePlatform.Services.Providers;

namespace WeatherIntelligencePlatform.Services;

public class WeatherProviderOrchestrator
{
    private readonly WeatherApiProvider _weatherApiProvider;
    private readonly ILogger<WeatherProviderOrchestrator> _logger;

    public WeatherProviderOrchestrator(
        WeatherApiProvider weatherApiProvider,
        ILogger<WeatherProviderOrchestrator> logger)
    {
        _weatherApiProvider = weatherApiProvider;
        _logger = logger;
    }

    // ===== ANLIK HAVA =====
    public async Task<NormalizedWeatherResult> GetWeatherAsync(string city)
    {
        try
        {
            var result = await _weatherApiProvider.GetWeatherAsync(city);
            _logger.LogInformation("Hava durumu {Provider} sağlayıcısından alındı: {City}", 
                _weatherApiProvider.ProviderName, city);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeatherAPI.com sağlayıcısı başarısız oldu: {City}", city);
            throw new Exception("Hava durumu alınamadı.", ex);
        }
    }

    // ===== TAHMİN =====
    public async Task<ForecastResponse> GetForecastAsync(string city)
    {
        try
        {
            var result = await _weatherApiProvider.GetForecastAsync(city);
            _logger.LogInformation("Tahmin {Provider} sağlayıcısından alındı: {City}", 
                _weatherApiProvider.ProviderName, city);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin alınamadı: {City}", city);
            throw;
        }
    }
}