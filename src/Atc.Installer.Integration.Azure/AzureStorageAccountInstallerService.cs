namespace Atc.Installer.Integration.Azure;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
public class AzureStorageAccountInstallerService : IAzureStorageAccountInstallerService
{
    public async Task<IList<FileInfo>> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        string[] componentNames)
    {
        ArgumentException.ThrowIfNullOrEmpty(storageConnectionString);
        ArgumentException.ThrowIfNullOrEmpty(blobContainerName);
        ArgumentException.ThrowIfNullOrEmpty(downloadFolder);
        ArgumentNullException.ThrowIfNull(componentNames);

        try
        {
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobContainerExist = await blobContainerClient
                .ExistsAsync()
                .ConfigureAwait(true);

            if (!blobContainerExist)
            {
                return new List<FileInfo>();
            }

            var blobNames = ListBlobsFlatListing(blobContainerClient)
                .OrderByDescending(x => x, StringComparer.Ordinal)
                .ToArray();

            var downloadedFiles = new List<FileInfo>();

            foreach (var componentName in componentNames)
            {
                var latestReleasesBlobName =
                    blobNames.FirstOrDefault(x => x.Contains(componentName, StringComparison.OrdinalIgnoreCase));
                if (latestReleasesBlobName == null)
                {
                    continue;
                }

                var blobClient = blobContainerClient.GetBlobClient(latestReleasesBlobName);
                var fileName = blobClient.Name.Split('/')[^1];
                var downloadFileForComponent = Path.Combine(downloadFolder, fileName);
                await blobClient
                    .DownloadToAsync(downloadFileForComponent)
                    .ConfigureAwait(true);

                downloadedFiles.Add(new FileInfo(Path.Combine(downloadFolder, fileName)));
            }

            return downloadedFiles;
        }
        catch
        {
            return new List<FileInfo>();
        }
    }

    private static IEnumerable<string> ListBlobsFlatListing(
        BlobContainerClient blobContainerClient)
        => GetBlobNames(GetBlobsAsPages(blobContainerClient));

    private static IEnumerable<Page<BlobItem>> GetBlobsAsPages(
        BlobContainerClient blobContainerClient)
        => blobContainerClient
            .GetBlobs()
            .AsPages(default, 100);

    private static IEnumerable<string> GetBlobNames(
        IEnumerable<Page<BlobItem>> blobPages)
    {
        var blobNames = new List<string>();

        foreach (var blobPage in blobPages)
        {
            blobNames.AddRange(blobPage.Values.Select(x => x.Name));
        }

        return blobNames;
    }
}