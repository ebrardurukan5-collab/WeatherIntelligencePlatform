using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace WeatherIntelligencePlatform.Services;

public class GeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeocodingService> _logger;
    private readonly string _apiKey;

    public GeocodingService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<GeocodingService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = configuration["WeatherApi:ApiKey"] 
            ?? throw new Exception("WeatherAPI.com API Key bulunamadı!");
    }

    public async Task<(double Latitude, double Longitude, string Name, string Country)> GetCoordinatesAsync(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            throw new ArgumentException("Şehir adı boş olamaz.");

        var cacheKey = $"geocode_{cityName.ToLower().Trim()}";

        if (_cache.TryGetValue(cacheKey, out (double Lat, double Lon, string Name, string Country) cached))
        {
            _logger.LogInformation("Geocoding cache'ten alındı: {City}", cityName);
            return cached;
        }

        var url = $"https://api.weatherapi.com/v1/search.json?key={_apiKey}&q={Uri.EscapeDataString(cityName)}";

        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Geocoding API hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new Exception($"Geocoding servisi çalışmıyor: {response.StatusCode}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var results = JsonSerializer.Deserialize<List<WeatherApiLocation>>(jsonString, options);

        if (results == null || results.Count == 0)
        {
            throw new Exception($"'{cityName}' şehri bulunamadı.");
        }

        var result = results[0];
        var coords = (result.Lat, result.Lon, result.Name, result.Country);

        _cache.Set(cacheKey, coords, TimeSpan.FromHours(1));
        _logger.LogInformation("Geocoding başarılı: {City} → ({Lat}, {Lon})", result.Name, result.Lat, result.Lon);

        return coords;
    }

    public async Task<string> GetPlaceNameFromCoordinatesAsync(double lat, double lon)
    {
        var cacheKey = $"geocode_reverse_{lat:F4}_{lon:F4}";

        if (_cache.TryGetValue(cacheKey, out string cachedName))
        {
            return cachedName!;
        }

        // WeatherAPI.com Reverse Geocoding - search endpoint ile
        var url = $"https://api.weatherapi.com/v1/search.json?key={_apiKey}&q={lat},{lon}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reverse geocoding başarısız: ({Lat}, {Lon}) - StatusCode: {StatusCode}", lat, lon, response.StatusCode);
                return "Bilinmeyen Bölge";
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var results = JsonSerializer.Deserialize<List<WeatherApiLocation>>(jsonString, options);

            if (results == null || results.Count == 0)
            {
                return "Bilinmeyen Bölge";
            }

            var name = results[0].Name ?? "Bilinmeyen Bölge";
            _cache.Set(cacheKey, name, TimeSpan.FromHours(1));
            return name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse geocoding hatası: ({Lat}, {Lon})", lat, lon);
            return "Bilinmeyen Bölge";
        }
    }

    private class WeatherApiLocation
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}