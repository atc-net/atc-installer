// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateNameValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is X509Certificate2 certificate)
        {
            if (string.IsNullOrEmpty(certificate.FriendlyName))
            {
                var index = certificate.IssuerName.Name.IndexOf('=', StringComparison.Ordinal);
                return index == -1
                    ? certificate.IssuerName.Name
                    : certificate.IssuerName.Name[(index + 1)..];
            }

            return certificate.FriendlyName;
        }

        return "N/A";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}