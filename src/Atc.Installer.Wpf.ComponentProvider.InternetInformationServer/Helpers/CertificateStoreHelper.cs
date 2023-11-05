namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer.Helpers;

public static class CertificateStoreHelper
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static async Task<X509Certificate2?> CreateSelfSignedCertificateAndAddToStore(
        string subjectName,
        string friendlyName,
        string dnsName,
        string password,
        int yearsUntilExpiry)
    {
        await Semaphore
            .WaitAsync()
            .ConfigureAwait(false);

        try
        {
            var selfSignedCertificate = CryptographyHelper.CreateSelfSignedX509CertificateForWebServer(
                subjectName,
                friendlyName,
                dnsName,
                password,
                yearsUntilExpiry);

            CryptographyHelper.AddCertificateToStore(selfSignedCertificate);

            CryptographyHelper.GrantAccessToCertificateToPrivateKey(selfSignedCertificate,
                Constants.WindowsAccounts.IssUser);

            CryptographyHelper.AddCertificateToStore(selfSignedCertificate, StoreName.Root);

            return selfSignedCertificate;
        }
        finally
        {
            Semaphore.Release();
        }
    }
}