// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateValidationInvalidToVisibilityVisibleValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is X509Certificate2 certificate)
        {
            return certificate.IsValid()
                ? Visibility.Hidden
                : Visibility.Visible;
        }

        return Visibility.Hidden;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}