using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherIntelligencePlatform.DTOs;

namespace WeatherIntelligencePlatform.Services.Providers;

public class WeatherApiProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    public string ProviderName => "WeatherAPI.com";

    public WeatherApiProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApi:ApiKey"] 
            ?? throw new Exception("WeatherAPI.com API Key bulunamadı!");
    }

    // ===== ANLIK HAVA =====
    public async Task<NormalizedWeatherResult> GetWeatherAsync(string city)
    {
        var url = $"https://api.weatherapi.com/v1/current.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&aqi=no";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<WeatherApiResponse>(jsonString, options);

        if (result?.Current == null || result.Location == null)
        {
            throw new Exception($"WeatherAPI.com: '{city}' için veri alınamadı.");
        }

        return new NormalizedWeatherResult
        {
            City = result.Location.Name ?? city,
            Country = result.Location.Country ?? "TR",
            Temperature = result.Current.TempC,
            FeelsLike = result.Current.FeelsLikeC,
            Humidity = result.Current.Humidity,
            WindSpeed = result.Current.WindKph / 3.6,
            Description = result.Current.Condition?.Text ?? "Bilinmiyor",
            Pressure = (int)Math.Round(result.Current.PressureMb),
            Visibility = (int)Math.Round(result.Current.VisKm * 1000)
        };
    }

    // ===== TAHMİN (3 GÜN, SAATLİK) =====
    public async Task<ForecastResponse> GetForecastAsync(string city, int days = 3)
    {
        var url = $"https://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&days={days}&aqi=no&alerts=no";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<ForecastResponse>(jsonString, options) ?? new ForecastResponse();
    }

    // ===== WEATHERAPI.COM RESPONSE DTO'ları =====
    private class WeatherApiResponse
    {
        public WeatherApiLocation Location { get; set; } = new();
        public WeatherApiCurrent Current { get; set; } = new();
    }

    private class WeatherApiLocation
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    private class WeatherApiCurrent
    {
        [JsonPropertyName("temp_c")]
        public double TempC { get; set; }

        [JsonPropertyName("feelslike_c")]
        public double FeelsLikeC { get; set; }

        public int Humidity { get; set; }

        [JsonPropertyName("wind_kph")]
        public double WindKph { get; set; }

        [JsonPropertyName("pressure_mb")]
        public double PressureMb { get; set; }

        [JsonPropertyName("vis_km")]
        public double VisKm { get; set; }

        public WeatherApiCondition Condition { get; set; } = new();
    }

    private class WeatherApiCondition
    {
        public string Text { get; set; } = string.Empty;
    }
}