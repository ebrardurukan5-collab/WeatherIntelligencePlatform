using System.Text;
using WeatherIntelligencePlatform.Models;
using WeatherIntelligencePlatform.Repositories; // ← EKLENDİ

namespace WeatherIntelligencePlatform.Services;

public class ReportService
{
    private readonly IWeatherRepository _repository;

    public ReportService(IWeatherRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GenerateCsvReport(string? city = null)
    {
        var logs = await _repository.GetAllLogsAsync();
        if (!string.IsNullOrEmpty(city))
            logs = logs.Where(l => l.City.ToLower() == city.ToLower()).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("City,Temperature,Humidity,WindSpeed,Description,Date");
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.City},{log.Temperature},{log.Humidity},{log.WindSpeed},{log.Description},{log.QueriedAt:yyyy-MM-dd HH:mm}");
        }
        return sb.ToString();
    }

    public async Task<string> GenerateHtmlReport(string? city = null)
    {
        var logs = await _repository.GetAllLogsAsync();
        if (!string.IsNullOrEmpty(city))
            logs = logs.Where(l => l.City.ToLower() == city.ToLower()).ToList();

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head><meta charset='UTF-8'><title>Hava Durumu Raporu</title>
        <style>
            body {{ font-family: Arial; padding: 20px; background: #f4f4f4; }}
            h1 {{ color: #00d4ff; }}
            table {{ width: 100%; border-collapse: collapse; background: white; }}
            th {{ background: #00d4ff; color: white; padding: 10px; }}
            td {{ padding: 8px; border-bottom: 1px solid #ddd; }}
            .summary {{ background: white; padding: 15px; border-radius: 10px; margin-bottom: 20px; }}
        </style>
        </head>
        <body>
            <h1>🌤️ Hava Durumu Raporu</h1>
            <div class='summary'>
                <p><strong>Toplam Sorgu:</strong> {logs.Count}</p>
                <p><strong>Ortalama Sıcaklık:</strong> {(logs.Any() ? Math.Round(logs.Average(l => l.Temperature), 1) : 0)}°C</p>
                <p><strong>Oluşturma Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>
            <table>
                <tr><th>Şehir</th><th>Sıcaklık</th><th>Nem</th><th>Rüzgar</th><th>Durum</th><th>Tarih</th></tr>
        ";

        foreach (var log in logs)
        {
            html += $"<tr><td>{log.City}</td><td>{log.Temperature}°C</td><td>{log.Humidity}%</td><td>{log.WindSpeed} km/s</td><td>{log.Description}</td><td>{log.QueriedAt:dd.MM.yyyy HH:mm}</td></tr>";
        }

        html += "</table></body></html>";
        return html;
    }
}