// ReSharper disable UseNullPropagation
namespace Atc.Installer.Wpf.ComponentProvider.ValueConverters;

public class PortFromUrlValueConverter : IValueConverter
{
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

        var s = value.ToString()!;
        var i1 = s.LastIndexOf(':') + 1;
        var i2 = s.IndexOf('/', i1);

        if (i1 > 0 && i2 == -1)
        {
            return s[i1..];
        }

        return i1 > 0 && i2 > 0
            ? s.Substring(i1, i2 - i1)
            : null;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}