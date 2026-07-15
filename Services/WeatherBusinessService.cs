using Microsoft.Extensions.Caching.Memory;
using WeatherIntelligencePlatform.DTOs;
using WeatherIntelligencePlatform.Models;
using WeatherIntelligencePlatform.Repositories;

namespace WeatherIntelligencePlatform.Services;

public class WeatherBusinessService
{
    private readonly WeatherProviderOrchestrator _orchestrator;
    private readonly IMemoryCache _cache;
    private readonly IWeatherRepository _weatherRepository;
    private readonly ILogger<WeatherBusinessService> _logger;

    public WeatherBusinessService(
        WeatherProviderOrchestrator orchestrator,
        IMemoryCache cache,
        IWeatherRepository weatherRepository,
        ILogger<WeatherBusinessService> logger)
    {
        _orchestrator = orchestrator;
        _cache = cache;
        _weatherRepository = weatherRepository;
        _logger = logger;
    }

    // ===== ANLIK HAVA =====
    public async Task<WeatherResponseDto> GetWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Şehir adı boş olamaz.");

        var cacheKey = $"weather_{city.ToLower().Trim()}";

        if (_cache.TryGetValue(cacheKey, out WeatherResponseDto cachedResult))
        {
            _logger.LogInformation("Cache'ten alındı: {City}", city);
            return cachedResult!;
        }

        var normalized = await _orchestrator.GetWeatherAsync(city);

        var result = new WeatherResponseDto
        {
            City = normalized.City ?? city,
            Country = normalized.Country ?? "TR",
            Temperature = normalized.Temperature,
            FeelsLike = normalized.FeelsLike > 0 ? normalized.FeelsLike : normalized.Temperature,
            Humidity = normalized.Humidity > 0 ? normalized.Humidity : 0,
            WindSpeed = normalized.WindSpeed > 0 ? normalized.WindSpeed : 0,
            Description = normalized.Description ?? "Bilinmiyor",
            Pressure = normalized.Pressure > 0 ? normalized.Pressure : 1015,
            Visibility = normalized.Visibility > 0 ? normalized.Visibility : 10000,
            ClothingAdvice = GetClothingAdvice(normalized.Temperature, normalized.WindSpeed),
            RiskLevel = GetRiskLevel(normalized.Temperature, normalized.WindSpeed, normalized.Description)
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        try
        {
            var logEntry = new WeatherQueryLog
            {
                City = result.City,
                Temperature = result.Temperature,
                Humidity = result.Humidity,
                WindSpeed = result.WindSpeed,
                Description = result.Description,
                QueriedAt = DateTime.UtcNow
            };
            await _weatherRepository.AddQueryLogAsync(logEntry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Veritabanına kayıt başarısız: {City}", city);
        }

        return result;
    }

    // ===== TAHMİN (3 GÜN, SAATLİK) =====
    public async Task<ForecastResponse> GetForecastAsync(string city)
    {
        var cacheKey = $"forecast_{city.ToLower().Trim()}";
        
        if (_cache.TryGetValue(cacheKey, out ForecastResponse cached))
            return cached!;

        var result = await _orchestrator.GetForecastAsync(city);
        _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    // ===== GEÇMİŞ =====
    public async Task<List<WeatherQueryLog>> GetQueryHistoryAsync()
    {
        return await _weatherRepository.GetAllLogsAsync();
    }

    public async Task<List<WeatherQueryLog>> GetRecentQueriesAsync(int count = 5)
    {
        return await _weatherRepository.GetRecentLogsAsync(count);
    }

    // ===== YARDIMCI METOTLAR =====
    private string GetClothingAdvice(double temp, double windSpeed)
    {
        if (temp < 5)
            return windSpeed > 30 ? "Kalın mont, atkı, bere ve rüzgarlık 🧥💨" : "Kalın mont, atkı ve bere 🧥";
        if (temp < 15)
            return windSpeed > 30 ? "Hırka/ceket ve rüzgarlık 🧣💨" : "Hırka veya ceket yeterli 🧣";
        if (temp < 25)
            return windSpeed > 30 ? "Hafif kıyafet ve rüzgarlık 👕💨" : "Hafif kıyafetler idealdir 👕";
        return windSpeed > 30 ? "İnce giyin, bol su iç ve rüzgarlık al 🥵💨" : "İnce ve rahat giyin, bol su iç 🥵";
    }

    private string GetRiskLevel(double temp, double windSpeed, string description)
    {
        if (windSpeed > 50 || temp < -5 || temp > 40) return "Yüksek";
        if (windSpeed > 30 || description.Contains("rain") || description.Contains("yağmur") ||
            description.Contains("snow") || description.Contains("kar")) return "Orta";
        return "Düşük";
    }
}