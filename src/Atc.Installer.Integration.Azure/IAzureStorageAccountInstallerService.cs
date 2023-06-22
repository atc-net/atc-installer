namespace Atc.Installer.Integration.Azure;

public interface IAzureStorageAccountInstallerService
{
    Task<IList<FileInfo>> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        IReadOnlyList<(string ComponentName, string? ContentHash)> components);
}