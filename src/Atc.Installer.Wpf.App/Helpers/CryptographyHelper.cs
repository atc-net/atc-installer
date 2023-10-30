namespace Atc.Installer.Wpf.App.Helpers;

public static class CryptographyHelper
{
    public static X509Certificate2 CreateSelfSignedCertificate(
        string subjectName,
        string password,
        int validDays = 365)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            $"CN={subjectName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature,
                critical: false));

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                critical: false));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(validDays));

        var pfxBytes = certificate.Export(X509ContentType.Pfx);

        return new X509Certificate2(
            pfxBytes,
            password,
            X509KeyStorageFlags.Exportable);
    }
}