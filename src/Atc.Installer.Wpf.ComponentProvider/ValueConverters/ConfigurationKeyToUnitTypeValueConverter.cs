namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

public class ConfigurationKeyToUnitTypeValueConverter : IValueConverter
{
    private static readonly IDictionary<string, string> UnitTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "MilliSeconds", "MilliSeconds" },
        { "Ms", "MilliSeconds" },
        { "Seconds", "Seconds" },
        { "Sec", "Seconds" },
        { "Minutes", "Minutes" },
        { "Min", "Minutes" },
        { "Hours", "Hours" },
        { "Hour", "Hour" },
        { "Days", "Days" },
        { "Months", "Months" },
        { "Years", "Years" },
        { "Time", "Time" },
        { "TimeUtc", "Time" },
    };

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        if (value is not string)
        {
            return null;
        }

        return TryParse(value.ToString()!, out var unitType)
            ? unitType
            : null;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotImplementedException();

    public static bool TryParse(
        string value, out string unitType)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        unitType = string.Empty;
        foreach (var item in UnitTypes)
        {
            if (!value.EndsWith(item.Key, StringComparison.Ordinal))
            {
                continue;
            }

            unitType = item.Value;
            return true;
        }

        return false;
    }

    public static string GetRegexPatternFromUnitType(
        string unitType)
    {
        ArgumentException.ThrowIfNullOrEmpty(unitType);

        return unitType switch
        {
            "MilliSeconds" or "Ms" or "Seconds" or "Minutes" or "Min" or "Hours" or "Hour" or
                "Days" or "Months" or "Years" => RegexPatternConstants.Numbers.PositiveOptional,
            "Time" or "TimeUtc" => RegexPatternConstants.Time.Optional,
            _ => string.Empty,
        };
    }
}