using Microsoft.Extensions.Caching.Memory;

namespace WeatherIntelligencePlatform.Services;

public class NotificationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IMemoryCache cache, ILogger<NotificationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SendWeatherAlert(string city, string message)
    {
        _logger.LogInformation("🔔 Bildirim: {City} - {Message}", city, message);
        await Task.CompletedTask;
    }

    public async Task CheckWeatherAlerts(string city, double temp, double wind, string desc)
    {
        var alerts = new List<string>();

        if (temp > 35) alerts.Add($"🔥 Aşırı sıcak! {temp}°C");
        if (temp < 0) alerts.Add($"🥶 Don tehlikesi! {temp}°C");
        if (wind > 50) alerts.Add($"💨 Fırtına! {wind} km/s");
        if (desc.Contains("rain") || desc.Contains("yağmur")) alerts.Add("🌧️ Yağmur yağıyor, şemsiye al!");
        if (desc.Contains("storm") || desc.Contains("fırtına")) alerts.Add("⛈️ Şiddetli fırtına! Dikkat!");
        if (desc.Contains("snow") || desc.Contains("kar")) alerts.Add("❄️ Kar yağıyor! Yollar kaygan!");

        foreach (var alert in alerts)
        {
            await SendWeatherAlert(city, alert);
        }
    }
}