using WeatherIntelligencePlatform.DTOs;

namespace WeatherIntelligencePlatform.Services;

public class RouteWeatherService
{
    private readonly GeocodingService _geocodingService;
    private readonly WeatherProviderOrchestrator _orchestrator;
    private readonly ILogger<RouteWeatherService> _logger;

    public RouteWeatherService(
        GeocodingService geocodingService,
        WeatherProviderOrchestrator orchestrator,
        ILogger<RouteWeatherService> logger)
    {
        _geocodingService = geocodingService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<RouteWeatherResponseDto> GetRouteWeatherAsync(string fromCity, string toCity)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            throw new ArgumentException("Başlangıç ve bitiş şehirleri belirtilmelidir.");

        // ===== SABİT ROTALAR (Koordinatlar + İngilizce İsimler) =====
        var routes = new Dictionary<string, (string Name, double Lat, double Lon, string ApiName)[]>
        {
            {
                "İstanbul,Ankara", new[]
                {
                    ("İstanbul", 41.0082, 28.9784, "Istanbul"),
                    ("Bilecik", 40.1433, 29.9792, "Bilecik"),
                    ("Eskişehir", 39.7767, 30.5206, "Eskisehir"),  // İngilizce yazım
                    ("Polatlı", 39.5772, 32.1417, "Polatli"),      // İngilizce yazım
                    ("Ankara", 39.9334, 32.8597, "Ankara")
                }
            },
            {
                "Ankara,İstanbul", new[]
                {
                    ("Ankara", 39.9334, 32.8597, "Ankara"),
                    ("Polatlı", 39.5772, 32.1417, "Polatli"),
                    ("Eskişehir", 39.7767, 30.5206, "Eskisehir"),
                    ("Bilecik", 40.1433, 29.9792, "Bilecik"),
                    ("İstanbul", 41.0082, 28.9784, "Istanbul")
                }
            },
            {
                "İstanbul,İzmir", new[]
                {
                    ("İstanbul", 41.0082, 28.9784, "Istanbul"),
                    ("Balıkesir", 39.6484, 27.8826, "Balikesir"),
                    ("Manisa", 38.6191, 27.4289, "Manisa"),
                    ("İzmir", 38.4192, 27.1287, "Izmir")
                }
            },
            {
                "Ankara,İzmir", new[]
                {
                    ("Ankara", 39.9334, 32.8597, "Ankara"),
                    ("Afyonkarahisar", 38.7638, 30.5403, "Afyonkarahisar"),
                    ("Manisa", 38.6191, 27.4289, "Manisa"),
                    ("İzmir", 38.4192, 27.1287, "Izmir")
                }
            }
        };

        var key = $"{fromCity},{toCity}";
        if (!routes.ContainsKey(key))
        {
            // Bilinmeyen rota için otomatik hesapla
            var (fromLat, fromLon, fromName, _) = await _geocodingService.GetCoordinatesAsync(fromCity);
            var (toLat, toLon, toName, _) = await _geocodingService.GetCoordinatesAsync(toCity);
            
            var totalDistance = CalculateDistance(fromLat, fromLon, toLat, toLon);
            var pointCount = Math.Max(2, Math.Min(5, (int)Math.Ceiling(totalDistance / 150) + 1));
            
            var stops = new List<RouteStopDto>();
            for (int i = 0; i < pointCount; i++)
            {
                double ratio = (double)i / (pointCount - 1);
                double lat = fromLat + (toLat - fromLat) * ratio;
                double lon = fromLon + (toLon - fromLon) * ratio;
                string name = i == 0 ? fromName : i == pointCount - 1 ? toName : $"Nokta {i}";
                
                try
                {
                    var weather = await _orchestrator.GetWeatherAsync(name);
                    stops.Add(CreateStop(name, lat, lon, weather, fromLat, fromLon));
                }
                catch
                {
                    stops.Add(CreateEmptyStop(name, lat, lon, fromLat, fromLon));
                }
            }
            
            return CreateResult(fromName, toName, stops);
        }

        var routePoints = routes[key];
        var stopsList = new List<RouteStopDto>();
        var (startLat, startLon, _, _) = await _geocodingService.GetCoordinatesAsync(fromCity);

        foreach (var (displayName, lat, lon, apiName) in routePoints)
        {
            try
            {
                // API'ye İngilizce isimle sorgula
                var weather = await _orchestrator.GetWeatherAsync(apiName);
                var distance = CalculateDistance(startLat, startLon, lat, lon);

                stopsList.Add(new RouteStopDto
                {
                    LocationName = displayName,
                    Latitude = lat,
                    Longitude = lon,
                    Temperature = weather.Temperature,
                    FeelsLike = weather.FeelsLike,
                    Humidity = weather.Humidity,
                    WindSpeed = weather.WindSpeed,
                    Description = weather.Description,
                    Pressure = weather.Pressure,
                    Visibility = weather.Visibility,
                    DistanceFromStart = Math.Round(distance, 1),
                    ClothingAdvice = GetClothingAdvice(weather.Temperature, weather.WindSpeed),
                    RiskLevel = GetRiskLevel(weather.Temperature, weather.WindSpeed, weather.Description)
                });
                
                _logger.LogInformation("✅ {Name} ({ApiName}) → {Temp}°C", displayName, apiName, weather.Temperature);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ {Name} ({ApiName}) için hava durumu alınamadı", displayName, apiName);
                
                var distance = CalculateDistance(startLat, startLon, lat, lon);
                stopsList.Add(new RouteStopDto
                {
                    LocationName = displayName,
                    Latitude = lat,
                    Longitude = lon,
                    Temperature = 0,
                    Description = "Veri alınamadı",
                    DistanceFromStart = Math.Round(distance, 1),
                    RiskLevel = "Bilinmiyor",
                    ClothingAdvice = "Bilinmiyor"
                });
            }
        }

        return CreateResult(fromCity, toCity, stopsList);
    }

    private RouteStopDto CreateStop(string name, double lat, double lon, NormalizedWeatherResult weather, double startLat, double startLon)
    {
        return new RouteStopDto
        {
            LocationName = name,
            Latitude = lat,
            Longitude = lon,
            Temperature = weather.Temperature,
            FeelsLike = weather.FeelsLike,
            Humidity = weather.Humidity,
            WindSpeed = weather.WindSpeed,
            Description = weather.Description,
            Pressure = weather.Pressure,
            Visibility = weather.Visibility,
            DistanceFromStart = Math.Round(CalculateDistance(startLat, startLon, lat, lon), 1),
            ClothingAdvice = GetClothingAdvice(weather.Temperature, weather.WindSpeed),
            RiskLevel = GetRiskLevel(weather.Temperature, weather.WindSpeed, weather.Description)
        };
    }

    private RouteStopDto CreateEmptyStop(string name, double lat, double lon, double startLat, double startLon)
    {
        return new RouteStopDto
        {
            LocationName = name,
            Latitude = lat,
            Longitude = lon,
            Temperature = 0,
            Description = "Veri alınamadı",
            DistanceFromStart = Math.Round(CalculateDistance(startLat, startLon, lat, lon), 1),
            RiskLevel = "Bilinmiyor",
            ClothingAdvice = "Bilinmiyor"
        };
    }

    private RouteWeatherResponseDto CreateResult(string fromCity, string toCity, List<RouteStopDto> stops)
    {
        var totalDistance = stops.LastOrDefault()?.DistanceFromStart ?? 0;
        var validTemps = stops.Where(s => s.Temperature != 0).Select(s => s.Temperature).ToList();

        return new RouteWeatherResponseDto
        {
            FromCity = fromCity,
            ToCity = toCity,
            TotalDistance = totalDistance,
            PointCount = stops.Count,
            Stops = stops,
            AverageTemperature = validTemps.Any() ? Math.Round(validTemps.Average(), 1) : 0,
            MinTemperature = validTemps.Any() ? Math.Round(validTemps.Min(), 1) : 0,
            MaxTemperature = validTemps.Any() ? Math.Round(validTemps.Max(), 1) : 0,
            OverallRisk = stops.Any(s => s.RiskLevel == "Yüksek") ? "Yüksek" :
                          stops.Any(s => s.RiskLevel == "Orta") ? "Orta" : "Düşük",
            Summary = GenerateSummary(stops, totalDistance)
        };
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    private string GetClothingAdvice(double temp, double windSpeed)
    {
        if (temp < 5) return windSpeed > 30 ? "🧥 Kalın mont + rüzgarlık" : "🧥 Kalın mont";
        if (temp < 15) return windSpeed > 30 ? "🧣 Hırka + rüzgarlık" : "🧣 Hırka";
        if (temp < 25) return windSpeed > 30 ? "👕 Hafif + rüzgarlık" : "👕 Hafif kıyafet";
        return windSpeed > 30 ? "🥵 İnce + rüzgarlık" : "🥵 İnce giyin";
    }

    private string GetRiskLevel(double temp, double windSpeed, string description)
    {
        if (windSpeed > 50 || temp < -5 || temp > 40) return "Yüksek";
        if (windSpeed > 30 || description.Contains("rain") || description.Contains("yağmur") ||
            description.Contains("snow") || description.Contains("kar")) return "Orta";
        return "Düşük";
    }

    private string GenerateSummary(List<RouteStopDto> stops, double totalDistance)
    {
        var temps = stops.Where(s => s.Temperature != 0).Select(s => s.Temperature).ToList();
        if (!temps.Any()) return "Veri alınamadı.";

        var summary = $"📍 {Math.Round(totalDistance, 1)} km • ";
        summary += $"🌡️ {Math.Round(temps.Average(), 1)}°C • ";
        summary += $"📈 {Math.Round(temps.Max(), 1)}°C • ";
        summary += $"📉 {Math.Round(temps.Min(), 1)}°C";

        if (stops.Any(s => s.Description.Contains("rain") || s.Description.Contains("yağmur")))
            summary += " • 🌂 Yağmur";
        if (stops.Any(s => s.Description.Contains("snow") || s.Description.Contains("kar")))
            summary += " • ❄️ Kar";

        return summary;
    }
}