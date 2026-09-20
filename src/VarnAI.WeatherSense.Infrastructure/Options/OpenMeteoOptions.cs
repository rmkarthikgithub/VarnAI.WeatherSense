namespace VarnAI.WeatherSense.Infrastructure.Options;

public class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    public string BaseUrl { get; set; } = "https://api.open-meteo.com/";
    public int DefaultForecastDays { get; set; } = 4;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxForecastDays { get; set; } = 16;
}
