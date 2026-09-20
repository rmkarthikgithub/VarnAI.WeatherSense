using FluentValidation;
using VarnAI.WeatherSense.Application.DTOs.Collections;

namespace VarnAI.WeatherSense.Application.Validators;

public class CollectWeatherRequestValidator : AbstractValidator<CollectWeatherRequest>
{
    public CollectWeatherRequestValidator()
    {
        RuleFor(x => x.ForecastDays)
            .InclusiveBetween(1, 16)
            .WithMessage("ForecastDays must be between 1 and 16.");
    }
}
