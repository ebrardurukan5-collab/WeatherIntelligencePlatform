namespace WeatherIntelligencePlatform.Models;

public class WeatherQueryLog
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime QueriedAt { get; set; }
}