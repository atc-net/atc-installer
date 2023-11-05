// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateValidationBrushValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is X509Certificate2 certificate)
        {
            return certificate.IsValid()
                ? Brushes.Transparent
                : Brushes.Red;
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}