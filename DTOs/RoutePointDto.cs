namespace WeatherIntelligencePlatform.DTOs;

public class RoutePointDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Temperature { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ClothingAdvice { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}