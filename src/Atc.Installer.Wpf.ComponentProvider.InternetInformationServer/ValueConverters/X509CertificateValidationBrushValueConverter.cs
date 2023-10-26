// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateValidationBrushValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is X509Certificate2 certificate)
        {
            return certificate.Archived ||
                   DateTime.Now >= certificate.NotAfter ||
                   DateTime.Now <= certificate.NotBefore ||
                   string.IsNullOrEmpty(certificate.FriendlyName)
                ? Brushes.Red
                : Brushes.Transparent;
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}