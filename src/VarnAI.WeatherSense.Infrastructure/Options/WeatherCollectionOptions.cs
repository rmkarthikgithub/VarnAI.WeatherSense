namespace VarnAI.WeatherSense.Infrastructure.Options;

public class WeatherCollectionOptions
{
    public const string SectionName = "WeatherCollection";

    public int DefaultForecastDays { get; set; } = 4;
    public bool StoreRawPayload { get; set; } = true;
}
