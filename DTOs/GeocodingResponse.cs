namespace WeatherIntelligencePlatform.DTOs;

public class GeocodingResponse
{
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Country { get; set; } = string.Empty;
}