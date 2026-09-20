using VarnAI.WeatherSense.Domain.Enums;

namespace VarnAI.WeatherSense.Application.Mappings;

public static class WeatherCodeMapper
{
    public static WeatherCondition ToCondition(int? code)
    {
        if (!code.HasValue) return WeatherCondition.Unknown;

        return code.Value switch
        {
            0 => WeatherCondition.Clear,
            1 => WeatherCondition.MainlyClear,
            2 => WeatherCondition.PartlyCloudy,
            3 => WeatherCondition.Cloudy,
            45 or 48 => WeatherCondition.Fog,
            51 or 53 or 55 or 56 or 57 => WeatherCondition.Drizzle,
            61 or 63 or 66 or 80 or 81 => WeatherCondition.Rain,
            65 or 67 or 82 => WeatherCondition.HeavyRain,
            71 or 73 or 75 or 77 or 85 or 86 => WeatherCondition.Snow,
            95 or 96 or 99 => WeatherCondition.Thunderstorm,
            _ => WeatherCondition.Unknown
        };
    }

    public static string ToDescription(int? code)
    {
        if (!code.HasValue) return "Unknown";

        return code.Value switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 => "Fog",
            48 => "Depositing rime fog",
            51 => "Drizzle: Light intensity",
            53 => "Drizzle: Moderate intensity",
            55 => "Drizzle: Dense intensity",
            56 => "Freezing Drizzle: Light",
            57 => "Freezing Drizzle: Dense",
            61 => "Rain: Slight intensity",
            63 => "Rain: Moderate intensity",
            65 => "Rain: Heavy intensity",
            66 => "Freezing Rain: Light",
            67 => "Freezing Rain: Heavy",
            71 => "Snow fall: Slight intensity",
            73 => "Snow fall: Moderate intensity",
            75 => "Snow fall: Heavy intensity",
            77 => "Snow grains",
            80 => "Rain showers: Slight",
            81 => "Rain showers: Moderate",
            82 => "Rain showers: Violent",
            85 => "Snow showers: Slight",
            86 => "Snow showers: Heavy",
            95 => "Thunderstorm: Slight or moderate",
            96 => "Thunderstorm with slight hail",
            99 => "Thunderstorm with heavy hail",
            _ => $"WMO Code {code.Value}"
        };
    }
}
