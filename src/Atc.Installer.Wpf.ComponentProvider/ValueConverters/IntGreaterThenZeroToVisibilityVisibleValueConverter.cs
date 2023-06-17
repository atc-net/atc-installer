namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

/// <summary>
/// ValueConverter: Int greater then zero To Visibility-Visible.
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public class IntGreaterThenZeroToVisibilityVisibleValueConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return Visibility.Collapsed;
        }

        if (value is not int intValue)
        {
            throw new UnexpectedTypeException($"Type {value.GetType().FullName} is not typeof(int)");
        }

        return intValue > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("This is a OneWay converter.");
    }
}