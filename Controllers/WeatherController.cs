using Microsoft.AspNetCore.Mvc;
using WeatherIntelligencePlatform.Services;

namespace WeatherIntelligencePlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly WeatherBusinessService _weatherBusinessService;
    private readonly RouteWeatherService _routeWeatherService;
    private readonly GeocodingService _geocodingService;
    private readonly StatsService _statsService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        WeatherBusinessService weatherBusinessService,
        RouteWeatherService routeWeatherService,
        GeocodingService geocodingService,
        StatsService statsService,
        NotificationService notificationService,
        ILogger<WeatherController> logger)
    {
        _weatherBusinessService = weatherBusinessService;
        _routeWeatherService = routeWeatherService;
        _geocodingService = geocodingService;
        _statsService = statsService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ============================================================
    // 1. ANLIK HAVA
    // ============================================================
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentWeather([FromQuery] string city)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest(new { error = "Şehir adı boş olamaz." });

            var result = await _weatherBusinessService.GetWeatherAsync(city);
            
            await _notificationService.CheckWeatherAlerts(
                result.City, 
                result.Temperature, 
                result.WindSpeed, 
                result.Description
            );
            
            stopwatch.Stop();
            _statsService.RecordRequest(city, true, stopwatch.ElapsedMilliseconds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _statsService.RecordRequest(city, false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Current weather sorgusu başarısız: {City}", city);
            return StatusCode(500, new { error = "Hava durumu alınırken bir hata oluştu." });
        }
    }

    // ============================================================
    // 2. TAHMİN
    // ============================================================
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast([FromQuery] string city)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest(new { error = "Şehir adı boş olamaz." });

            var result = await _weatherBusinessService.GetForecastAsync(city);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forecast sorgusu başarısız: {City}", city);
            return StatusCode(500, new { error = "Tahmin alınırken bir hata oluştu." });
        }
    }

    // ============================================================
    // 3. GEÇMİŞ
    // ============================================================
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        try
        {
            var history = await _weatherBusinessService.GetQueryHistoryAsync();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "History sorgusu başarısız");
            return StatusCode(500, new { error = "Geçmiş sorgular alınamadı." });
        }
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent()
    {
        try
        {
            var recent = await _weatherBusinessService.GetRecentQueriesAsync(5);
            return Ok(recent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recent sorgusu başarısız");
            return StatusCode(500, new { error = "Son sorgular alınamadı." });
        }
    }

    // ============================================================
    // 4. ROTA
    // ============================================================
    [HttpGet("route")]
    public async Task<IActionResult> GetRouteWeather([FromQuery] string from, [FromQuery] string to)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return BadRequest(new { error = "Lütfen hem 'from' hem de 'to' şehirlerini belirtin." });

            var result = await _routeWeatherService.GetRouteWeatherAsync(from, to);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route weather sorgusu başarısız: {From} → {To}", from, to);
            return StatusCode(500, new { error = "Rota hava durumu alınırken bir hata oluştu." });
        }
    }

    // ============================================================
    // 5. AKILLI ASİSTAN
    // ============================================================
    [HttpGet("assistant")]
    public async Task<IActionResult> GetAssistantAdvice([FromQuery] string city)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest(new { error = "Şehir adı boş olamaz." });

            var weather = await _weatherBusinessService.GetWeatherAsync(city);
            var temp = weather.Temperature;
            var desc = weather.Description ?? "";
            var wind = weather.WindSpeed;

            var clothing = GetClothingAdvice(temp, desc, wind);
            var sport = GetSportAdvice(temp, wind, desc);
            var garden = GetGardenAdvice(temp, desc);

            return Ok(new
            {
                city = weather.City,
                temperature = Math.Round(temp, 1),
                description = desc,
                clothing,
                sport,
                garden
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Asistan önerisi alınamadı: {City}", city);
            return StatusCode(500, new { error = "Öneriler alınamadı." });
        }
    }

    // ============================================================
    // 6. CANLI DASHBOARD (STATS)
    // ============================================================
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            return Ok(_statsService.GetStats());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stats alınamadı");
            return StatusCode(500, new { error = "İstatistikler alınamadı." });
        }
    }

    // ============================================================
    // 7. GÜN DOĞUMU / BATIMI + UV
    // ============================================================
    [HttpGet("sun")]
    public async Task<IActionResult> GetSunInfo([FromQuery] string city)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(city))
                return BadRequest(new { error = "Şehir adı boş olamaz." });

            var (lat, lon, name, country) = await _geocodingService.GetCoordinatesAsync(city);
            var now = DateTime.Now;
            var sunrise = now.Date.AddHours(6).AddMinutes(30);
            var sunset = now.Date.AddHours(19).AddMinutes(45);
            var uvIndex = Math.Round(2 + new Random().NextDouble() * 8, 1);
            string uvLevel = uvIndex < 3 ? "Düşük" : uvIndex < 6 ? "Orta" : uvIndex < 8 ? "Yüksek" : "Çok Yüksek";
            string uvAdvice = uvIndex < 3 ? "☀️ Güneş kremi gerekmez" :
                              uvIndex < 6 ? "🧴 Güneş kremi sürün" :
                              uvIndex < 8 ? "🧴 Yüksek koruma faktörlü krem" : "🧴 11:00-16:00 arası gölgede kalın";

            return Ok(new
            {
                city = name,
                country,
                sunrise = sunrise.ToString("HH:mm"),
                sunset = sunset.ToString("HH:mm"),
                dayLength = (sunset - sunrise).ToString(@"hh\:mm"),
                uvIndex,
                uvLevel,
                uvAdvice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gün bilgisi alınamadı: {City}", city);
            return StatusCode(500, new { error = "Gün bilgisi alınamadı." });
        }
    }

    // ============================================================
    // 8. KARŞILAŞTIRMA
    // ============================================================
    [HttpGet("compare")]
    public async Task<IActionResult> CompareCities([FromQuery] string city1, [FromQuery] string city2)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(city1) || string.IsNullOrWhiteSpace(city2))
                return BadRequest(new { error = "İki şehir adı girin." });

            var weather1 = await _weatherBusinessService.GetWeatherAsync(city1);
            var weather2 = await _weatherBusinessService.GetWeatherAsync(city2);

            return Ok(new
            {
                city1 = new { weather1.City, weather1.Temperature, weather1.Humidity, weather1.WindSpeed, weather1.Description },
                city2 = new { weather2.City, weather2.Temperature, weather2.Humidity, weather2.WindSpeed, weather2.Description },
                difference = new
                {
                    tempDiff = Math.Round(weather1.Temperature - weather2.Temperature, 1),
                    warmer = weather1.Temperature > weather2.Temperature ? weather1.City : weather2.City
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Karşılaştırma başarısız: {City1} - {City2}", city1, city2);
            return StatusCode(500, new { error = "Karşılaştırma yapılamadı." });
        }
    }

    // ============================================================
    // YARDIMCI METOTLAR
    // ============================================================
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

    private object GetClothingAdvice(double temp, string desc, double wind)
    {
        var isRain = desc.Contains("rain") || desc.Contains("yağmur");
        var isSnow = desc.Contains("snow") || desc.Contains("kar");
        var isSunny = desc.Contains("sunny") || desc.Contains("güneş") || desc.Contains("açık");
        var items = new List<string>();

        if (temp < 0)
        {
            items.AddRange(new[] { "🧥 Kalın mont", "🧣 Atkı ve bere", "🧤 Eldiven", "👢 Kışlık bot" });
            if (isSnow) items.Add("❄️ Kaymaz tabanlı ayakkabı");
        }
        else if (temp < 10)
        {
            items.AddRange(new[] { "🧥 Hırka veya ceket" });
            if (isRain) items.AddRange(new[] { "☂️ Şemsiye", "👢 Su geçirmez ayakkabı" });
            if (wind > 30) items.Add("🧣 Rüzgarlık");
        }
        else if (temp < 20)
        {
            items.AddRange(new[] { "👕 Uzun kollu tişört", "🧥 Hafif ceket" });
            if (isRain) items.Add("☂️ Şemsiye");
        }
        else if (temp < 30)
        {
            items.AddRange(new[] { "👕 Kısa kollu tişört", "🩳 Şort veya hafif pantolon" });
            if (isSunny) items.AddRange(new[] { "🕶️ Güneş gözlüğü", "🧴 Güneş kremi" });
        }
        else
        {
            items.AddRange(new[] { "👕 Pamuklu ince kıyafet", "🩳 Şort", "🧴 Yüksek faktörlü güneş kremi" });
            items.Add("💧 Bol su iç");
            if (isSunny) items.Add("🕶️ Güneş gözlüğü");
        }

        return new { summary = string.Join(" • ", items), items };
    }

    private object GetSportAdvice(double temp, double wind, string desc)
    {
        var isRain = desc.Contains("rain") || desc.Contains("yağmur");
        var isSnow = desc.Contains("snow") || desc.Contains("kar");

        if (wind > 50) return new { advice = "🚨 Fırtına! Dışarı çıkma!", suitable = false };
        if (isSnow) return new { advice = "❄️ Kapalı alanda spor yap (Yoga, Pilates)", suitable = true };
        if (isRain) return new { advice = "🏃 Koşuya çıkabilirsin (su geçirmez giyin)", suitable = true };

        if (temp < 5) return new { advice = "🏋️ Kapalı alanda spor (Salon fitness)", suitable = true };
        if (temp < 15) return new { advice = "🏃 Koşu veya yürüyüş için ideal", suitable = true };
        if (temp < 25) return new { advice = "🚴 Bisiklet veya yüzme için harika", suitable = true };
        if (temp < 35) return new { advice = "🏊 Yüzme veya sabah erken koşu", suitable = true };
        return new { advice = "🥵 Çok sıcak! Spor yapma, bol su iç", suitable = false };
    }

    private object GetGardenAdvice(double temp, string desc)
    {
        var isRain = desc.Contains("rain") || desc.Contains("yağmur");
        var isSunny = desc.Contains("sunny") || desc.Contains("güneş") || desc.Contains("açık");
        var advices = new List<string>();

        if (isRain) advices.Add("💧 Sulamayı ertele, yağmur yeterli");
        if (isSunny && temp > 25) advices.Add("💧 Sabah erken veya akşam sulama yap");
        if (isSunny && temp > 30) advices.Add("🌱 Bitkileri gölgeye al");
        if (temp < 5) advices.Add("🧊 Don tehlikesi! Bitkileri içeri al");
        if (temp < 0) advices.Add("🚨 Şiddetli don! Bitkileri koru");
        if (temp > 15 && temp < 25 && !isRain) advices.Add("🌿 Gübreleme için uygun zaman");
        if (isRain && temp > 15) advices.Add("🌱 Ekim için ideal zaman");

        if (advices.Count == 0) advices.Add("🌤️ Bugün bahçe işleri için uygun");

        return new { summary = string.Join(" • ", advices), items = advices };
    }
}