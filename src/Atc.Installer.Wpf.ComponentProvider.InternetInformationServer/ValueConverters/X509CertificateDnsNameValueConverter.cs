// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.ValueConverters;

public class X509CertificateDnsNameValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not X509Certificate2 certificate)
        {
            return "N/A";
        }

        foreach (var extension in certificate.Extensions)
        {
            if (extension.Oid?.Value != "2.5.29.17")
            {
                continue;
            }

            var sanExtension = new X509SubjectAlternativeNameExtension(extension.RawData);
            var dnsNames = sanExtension
                .EnumerateDnsNames()
                .ToList();

            return string.Join(", ", dnsNames);
        }

        return "N/A";
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