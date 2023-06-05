namespace Atc.Installer.Integration.Azure;

public class AzureStorageAccountInstallerService : IAzureStorageAccountInstallerService
{
    private static readonly object InstanceLock = new();
    private static AzureStorageAccountInstallerService? instance;

    private AzureStorageAccountInstallerService()
    {
    }

    public static AzureStorageAccountInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new AzureStorageAccountInstallerService();
            }
        }
    }

    public IList<FileInfo> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        string[] componentNames)
    {
        ArgumentException.ThrowIfNullOrEmpty(storageConnectionString);

        var blobServiceClient = new BlobServiceClient(storageConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
        var blobContainerExist = blobContainerClient.Exists();
        if (!blobContainerExist)
        {
            return new List<FileInfo>();
        }

        throw new NotImplementedException();
    }
}