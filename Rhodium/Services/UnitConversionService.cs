using Microsoft.EntityFrameworkCore;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Results;
using Rhodium.Data;

namespace Rhodium.Services;

public class UnitConversionService(IDbContextFactory<RhodiumContext> dbContextFactory)
{
    private readonly NumberWithUnitRecognizer numbersRecognizer = new(Culture.English);

    public async Task<Result<IReadOnlyList<UnitConversion>>> GetConversionsForMessageAsync(IPartialMessage message)
    {
        var messageContent = message.Content.Value;

        if (string.IsNullOrEmpty(messageContent))
        {
            return Result<IReadOnlyList<UnitConversion>>.FromSuccess([]);
        }

        await using RhodiumContext context = await dbContextFactory.CreateDbContextAsync();

        var userConfig = await context.UserConfigs.FindAsync(message.Author.Value.ID);

        if (userConfig is null)
        {
            return new RhodiumNotConfiguredError();
        }

        var conversions = new List<UnitConversion>();
        var temperatures = numbersRecognizer.GetTemperatureModel().Parse(messageContent);
        var measurements = numbersRecognizer.GetDimensionModel().Parse(messageContent);

        foreach (var temperature in temperatures)
        {
            if (temperature.TypeName is not "temperature")
            {
                continue;
            }

            string unit = temperature.Resolution["unit"].ToString()!;
            string value = temperature.Resolution["value"].ToString()!;

            var convertedValue = this.ConvertTemperature(value, unit, userConfig.PreferredTemperatureUnit);

            if (!convertedValue.IsSuccess)
            {
                continue;
            }

            conversions.Add(new UnitConversion($"`{temperature.Text}`", $"{convertedValue.Entity:N2}{userConfig.PreferredTemperatureUnit.ToString()[0]}"));
        }


        foreach (var measurement in measurements)
        {
            if (measurement.TypeName is not "dimension")
            {
                continue;
            }

            string unit = measurement.Resolution["unit"].ToString()!;
            string value = measurement.Resolution["value"].ToString()!;

            var valueConversionResult = this.ConvertMeasurement(value, unit, userConfig.PreferredMeasurementUnit);

            if (!valueConversionResult.IsDefined(out var convertedValue))
            {
                continue;
            }

            conversions.Add(new UnitConversion($"`{measurement.Text}`", $"{convertedValue.Item1:N2} {convertedValue.Item2}"));
        }

        return conversions;
    }

    private Result<(double, string)> ConvertMeasurement(string value, string unit, RhodiumMeasurement preferredUnit)
    {
        if (!double.TryParse(value, out double measurement))
        {
            return Result<(double, string)>.FromError(new ParsingError<double>("Failed to parse measurement value."));
        }

        return (unit, preferredUnit) switch
        {
            ("Meter", RhodiumMeasurement.Imperial) => (measurement * 3.28084, "ft"),
            ("Centimeter", RhodiumMeasurement.Imperial) => (measurement * 0.0328084, "in"),
            ("Gram", RhodiumMeasurement.Imperial) => (measurement * 0.00220462, "lb"),
            ("Kilogram", RhodiumMeasurement.Imperial) => (measurement * 2.20462 , "lb"),
            ("Foot", RhodiumMeasurement.Metric) => (measurement * 0.3048, "m"),
            ("Inch", RhodiumMeasurement.Metric) => (measurement * 2.54, "cm"),
            ("Pound", RhodiumMeasurement.Metric) => (measurement * 0.453592, "kg"),
            ("Mile", RhodiumMeasurement.Metric) => (measurement * 1.60934, "km"),
            ("Kilometer", RhodiumMeasurement.Imperial) => (measurement * 0.621371, "mi"),
            (_, RhodiumMeasurement.Imperial) => Result<(double, string)>.FromError(new InvalidOperationError("Placeholder error because the units are the same")),
            (_, RhodiumMeasurement.Metric) => Result<(double, string)>.FromError(new InvalidOperationError("Placeholder error because the units are the same")),
            _ => throw new ArgumentOutOfRangeException(nameof(preferredUnit), preferredUnit, "Invalid measurement unit.")
        };
    }

    private Result<double> ConvertTemperature(string value, string unit, RhodiumTemperature preferredUnit)
    {
        if (!double.TryParse(value, out double temperature))
        {
            return Result<double>.FromError(new ParsingError<double>("Failed to parse temperature value."));
        }

        return (unit, preferredUnit) switch
        {
            ("Celsius", RhodiumTemperature.Celsius) => Result<double>.FromError(new InvalidOperationError("Placeholder error becuase the units are the same")),
            ("Fahrenheit", RhodiumTemperature.Fahrenheit) => Result<double>.FromError(new InvalidOperationError("Placeholder error becuase the units are the same")),
            ("Celsius", RhodiumTemperature.Fahrenheit) => temperature * 9 / 5 + 32,
            ("Fahrenheit", RhodiumTemperature.Celsius) => (temperature - 32) * 5 / 9,
            ("Degree", RhodiumTemperature.Celsius) => ConvertTemperature(value, "Fahrenheit", RhodiumTemperature.Celsius).Entity,
            ("Degree", RhodiumTemperature.Fahrenheit) => ConvertTemperature(value, "Celsius", RhodiumTemperature.Fahrenheit).Entity,
            _ => throw new ArgumentOutOfRangeException(nameof(preferredUnit), preferredUnit, "Invalid temperature unit.")
        };
    }
}

/// <summary>
/// Represents a conversion from one unit to another.
/// </summary>
/// <param name="OriginalUnit">The original unit value.</param>
/// <param name="ConvertedUnit">The converted unit value.</param>
public record UnitConversion(string OriginalUnit, string ConvertedUnit);
