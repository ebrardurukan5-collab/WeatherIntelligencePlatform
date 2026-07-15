using WeatherIntelligencePlatform.Models;

namespace WeatherIntelligencePlatform.Repositories;

public interface IWeatherRepository
{
    Task AddQueryLogAsync(WeatherQueryLog log);
    Task<List<WeatherQueryLog>> GetAllLogsAsync();
    Task<List<WeatherQueryLog>> GetRecentLogsAsync(int count);
}