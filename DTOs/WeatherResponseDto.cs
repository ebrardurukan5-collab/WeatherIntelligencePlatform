namespace WeatherIntelligencePlatform.DTOs;

public class WeatherResponseDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ClothingAdvice { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int Pressure { get; set; }
    public int Visibility { get; set; }
}