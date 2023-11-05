// ReSharper disable CheckNamespace
namespace System.Security.Cryptography.X509Certificates;

public static class X509Certificate2Extensions
{
    public static string GetNameIdentifier(
        this X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (!string.IsNullOrEmpty(certificate.FriendlyName))
        {
            return certificate.FriendlyName;
        }

        const string searchText = "CN=";
        var index = certificate.SubjectName.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        return index == -1
            ? certificate.SubjectName.Name
            : certificate.SubjectName.Name[(index + searchText.Length)..];
    }

    public static bool IsValid(
        this X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        return !certificate.Archived &&
               DateTime.Now < certificate.NotAfter &&
               DateTime.Now > certificate.NotBefore &&
               !string.IsNullOrEmpty(certificate.GetNameIdentifier());
    }
}