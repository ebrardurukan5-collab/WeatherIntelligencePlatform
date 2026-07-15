namespace WeatherIntelligencePlatform.DTOs;

public class RouteWeatherResponseDto
{
    public string FromCity { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public double TotalDistance { get; set; }
    public int PointCount { get; set; }
    public List<RouteStopDto> Stops { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string OverallRisk { get; set; } = string.Empty;
    public double AverageTemperature { get; set; }
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
}