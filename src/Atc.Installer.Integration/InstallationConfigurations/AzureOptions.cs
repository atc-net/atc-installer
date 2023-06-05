namespace Atc.Installer.Integration.InstallationConfigurations;

public class AzureOptions
{
    public const string SectionName = "Azure";

    public string StorageConnectionString { get; set; } = string.Empty;

    public string BlobContainerName { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(StorageConnectionString)}: {StorageConnectionString}, {nameof(BlobContainerName)}: {BlobContainerName}";
}