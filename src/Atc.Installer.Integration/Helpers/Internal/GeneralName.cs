namespace Atc.Installer.Integration.Helpers.Internal;

internal class GeneralName
{
    public string DnsName { get; set; } = string.Empty;

    public byte[] RawData
    {
        get
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(DnsName);
            return sanBuilder.Build().RawData;
        }
    }
}