namespace WeatherIntelligencePlatform.DTOs;

public class RouteRequestDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int? PointCount { get; set; }
}