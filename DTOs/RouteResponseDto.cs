namespace WeatherIntelligencePlatform.DTOs;

public class RouteResponseDto
{
    public List<RoutePointDto> RoutePoints { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string OverallRisk { get; set; } = string.Empty;
    public double AverageTemperature { get; set; }
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
}