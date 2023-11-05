// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateNameValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => value is X509Certificate2 certificate
            ? certificate.GetNameIdentifier()
            : "N/A";

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}