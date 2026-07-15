namespace WeatherIntelligencePlatform.DTOs;

public class RouteStopDto
{
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Pressure { get; set; }
    public int Visibility { get; set; }
    public double DistanceFromStart { get; set; }
    public string ClothingAdvice { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}