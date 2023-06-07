namespace Atc.Installer.Integration.Azure;

public interface IAzureStorageAccountInstallerService
{
    Task<IList<FileInfo>> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        string[] componentNames);
}