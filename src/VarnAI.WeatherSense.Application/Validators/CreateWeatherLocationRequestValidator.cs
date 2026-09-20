using FluentValidation;
using VarnAI.WeatherSense.Application.DTOs.Locations;

namespace VarnAI.WeatherSense.Application.Validators;

public class CreateWeatherLocationRequestValidator : AbstractValidator<CreateWeatherLocationRequest>
{
    public CreateWeatherLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required.")
            .MaximumLength(100).WithMessage("Location name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Location code is required.")
            .MaximumLength(50).WithMessage("Location code must not exceed 50 characters.")
            .Matches("^[A-Z0-9_-]+$").WithMessage("Location code must contain only uppercase letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude must be between -90 and 90 degrees.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude must be between -180 and 180 degrees.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone identifier is required.")
            .Must(IsValidIanaTimezone)
            .WithMessage("Timezone must be a valid IANA timezone identifier (e.g. 'Asia/Kolkata').");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .MaximumLength(50).WithMessage("Provider must not exceed 50 characters.");
    }

    private static bool IsValidIanaTimezone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone)) return false;
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
