using System.Text.Json.Serialization;

namespace WeatherIntelligencePlatform.DTOs;

public class ForecastResponse
{
    public ForecastLocation Location { get; set; } = new();
    public ForecastCurrent Current { get; set; } = new();
    public ForecastForecast Forecast { get; set; } = new();
}

public class ForecastLocation
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class ForecastCurrent
{
    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("feelslike_c")]
    public double FeelsLikeC { get; set; }

    public int Humidity { get; set; }

    [JsonPropertyName("wind_kph")]
    public double WindKph { get; set; }

    [JsonPropertyName("pressure_mb")]
    public double PressureMb { get; set; }

    [JsonPropertyName("vis_km")]
    public double VisKm { get; set; }

    public ForecastCondition Condition { get; set; } = new();
}

public class ForecastCondition
{
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ForecastForecast
{
    [JsonPropertyName("forecastday")]
    public List<ForecastDay> ForecastDays { get; set; } = new();
}

public class ForecastDay
{
    public string Date { get; set; } = string.Empty;
    public ForecastDayData Day { get; set; } = new();
    public List<ForecastHourData> Hour { get; set; } = new();
}

public class ForecastDayData
{
    [JsonPropertyName("maxtemp_c")]
    public double MaxTempC { get; set; }

    [JsonPropertyName("mintemp_c")]
    public double MinTempC { get; set; }

    public ForecastCondition Condition { get; set; } = new();
}

public class ForecastHourData
{
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("feelslike_c")]
    public double FeelsLikeC { get; set; }

    public int Humidity { get; set; }

    [JsonPropertyName("wind_kph")]
    public double WindKph { get; set; }

    public ForecastCondition Condition { get; set; } = new();

    [JsonPropertyName("chance_of_rain")]
    public int ChanceOfRain { get; set; }

    [JsonPropertyName("chance_of_snow")]
    public int ChanceOfSnow { get; set; }
}