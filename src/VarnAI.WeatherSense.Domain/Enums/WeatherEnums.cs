namespace VarnAI.WeatherSense.Domain.Enums;

public enum WeatherCondition
{
    Unknown = 0,
    Clear = 1,
    MainlyClear = 2,
    PartlyCloudy = 3,
    Cloudy = 4,
    Fog = 5,
    Drizzle = 6,
    Rain = 7,
    HeavyRain = 8,
    Snow = 9,
    Thunderstorm = 10
}

public enum CollectionExecutionStatus
{
    Started = 1,
    Completed = 2,
    CompletedWithErrors = 3,
    Failed = 4
}

public static class CollectionTriggerSource
{
    public const string N8n = "n8n";
    public const string Manual = "Manual";
    public const string Api = "API";
}

public static class WeatherDataSource
{
    public const string OpenMeteo = "OpenMeteo";
    public const string IoTSensor = "IoTSensor";
    public const string Manual = "Manual";
    public const string OtherProvider = "OtherProvider";
}
