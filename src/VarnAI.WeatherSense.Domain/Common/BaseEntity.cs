namespace VarnAI.WeatherSense.Domain.Common;

public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
