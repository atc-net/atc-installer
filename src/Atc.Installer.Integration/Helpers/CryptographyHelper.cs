// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Atc.Installer.Integration.Helpers;

public static class CryptographyHelper
{
    public static IList<X509Certificate2> GetX509Certificates(
        StoreName storeName = StoreName.My,
        StoreLocation storeLocation = StoreLocation.LocalMachine,
        bool validOnly = true)
    {
        IList<X509Certificate2> certificates = new List<X509Certificate2>();

        using var store = new X509Store(
            storeName,
            storeLocation);

        store.Open(OpenFlags.ReadOnly);

        foreach (var certificate in store.Certificates)
        {
            if (validOnly && !certificate.IsValid())
            {
                continue;
            }

            certificates.Add(certificate);
        }

        store.Close();

        return certificates;
    }

    public static X509Certificate2? FindX509Certificate(
        string certificateHash,
        StoreName storeName = StoreName.My,
        StoreLocation storeLocation = StoreLocation.LocalMachine,
        bool validOnly = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificateHash);

        using var store = new X509Store(
            storeName,
            storeLocation);

        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            findValue: certificateHash,
            validOnly);

        store.Close();

        return certificates.Count == 1
            ? certificates[0]
            : null;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    [SupportedOSPlatform("Windows")]
    public static X509Certificate2 CreateSelfSignedX509CertificateForWebServer(
        string subjectName,
        string friendlyName,
        string dnsName,
        string password,
        int yearsUntilExpiry = 1,
        X509KeyUsageFlags x509KeyUsageFlags = X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature,
        X509KeyStorageFlags x509KeyStorageFlags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable)
    {
        ArgumentException.ThrowIfNullOrEmpty(subjectName);
        ArgumentException.ThrowIfNullOrEmpty(friendlyName);
        ArgumentException.ThrowIfNullOrEmpty(dnsName);
        ArgumentException.ThrowIfNullOrEmpty(password);

        // Create a strong cryptographic algorithm for the certificate's key
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName($"CN={subjectName}");

        var request = new CertificateRequest(
            subject,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Set the subject alternate name
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: false));

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                critical: false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                x509KeyUsageFlags,
                critical: false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    // OID for TLS Web Server Authentication
                    new("1.3.6.1.5.5.7.3.1"),

                    // OID for TLS Web Client Authentication
                    new("1.3.6.1.5.5.7.3.2"),
                },
                critical: true));

        // Set the DNS name
        request.CertificateExtensions.Add(
            new X509Extension(
                encodedExtension: new AsnEncodedData(
                    new Oid("2.5.29.17"),
                    new GeneralName { DnsName = dnsName }.RawData),
                critical: false));

        // Create the certificate and set a password for the PFX
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(yearsUntilExpiry));

        // Set the friendly name
        certificate.FriendlyName = friendlyName;

        // Export the certificate with a private key and password protection
        var pfxBytes = certificate.Export(
            X509ContentType.Pfx,
            password);

        // Convert the exported bytes back to an X509Certificate2 object
        return new X509Certificate2(
            pfxBytes,
            password,
            x509KeyStorageFlags);
    }

    public static void AddCertificateToStore(
        X509Certificate2 certificate,
        StoreName storeName = StoreName.My,
        StoreLocation storeLocation = StoreLocation.LocalMachine)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        using var store = new X509Store(
            storeName,
            storeLocation);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
        store.Close();
    }

    [SupportedOSPlatform("Windows")]
    public static void GrantAccessToCertificateToPrivateKey(
        X509Certificate2 certificate,
        string accountName)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrEmpty(accountName);

        if (!certificate.HasPrivateKey ||
            certificate.GetRSAPrivateKey() is not RSACng rsa)
        {
            throw new CertificateValidationException("The certificate does not have a private key.");
        }

        var uniqueKeyContainerName = rsa.Key.UniqueName!;

        var machineKeysFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Microsoft\Crypto\Keys";

        var fileInfo = new FileInfo(Path.Combine(machineKeysFolder, uniqueKeyContainerName));
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Multiple private key files found.");
        }

        var fileSecurity = fileInfo.GetAccessControl();

        var account = new NTAccount(accountName);
        var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

        var accessRule = new FileSystemAccessRule(
            sid,
            FileSystemRights.FullControl,
            AccessControlType.Allow);

        fileSecurity.AddAccessRule(accessRule);

        fileInfo.SetAccessControl(fileSecurity);
    }
}