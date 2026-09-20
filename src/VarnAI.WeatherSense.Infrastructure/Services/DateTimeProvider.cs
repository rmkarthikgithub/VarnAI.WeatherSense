using VarnAI.WeatherSense.Application.Common.Interfaces;

namespace VarnAI.WeatherSense.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
