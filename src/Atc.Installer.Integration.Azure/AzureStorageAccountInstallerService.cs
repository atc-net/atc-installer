// ReSharper disable LoopCanBeConvertedToQuery
namespace Atc.Installer.Integration.Azure;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
public class AzureStorageAccountInstallerService : IAzureStorageAccountInstallerService
{
    public async Task<IList<FileInfo>> DownloadLatestFilesByNames(
        string storageConnectionString,
        string blobContainerName,
        string downloadFolder,
        IReadOnlyList<(string ComponentName, string? ContentHash)> components)
    {
        ArgumentException.ThrowIfNullOrEmpty(storageConnectionString);
        ArgumentException.ThrowIfNullOrEmpty(blobContainerName);
        ArgumentException.ThrowIfNullOrEmpty(downloadFolder);
        ArgumentNullException.ThrowIfNull(components);

        var blobServiceClient = new BlobServiceClient(storageConnectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
        var blobContainerExist = await blobContainerClient
            .ExistsAsync()
            .ConfigureAwait(true);

        if (!blobContainerExist)
        {
            return new List<FileInfo>();
        }

        var blobsToDownload = GetBlobsToDownload(blobContainerClient, components);
        return blobsToDownload.Count > 0
            ? await HandleFileDownloads(
                    downloadFolder,
                    blobsToDownload,
                    blobContainerClient)
                .ConfigureAwait(true)
            : new List<FileInfo>();
    }

    private static List<string> GetBlobsToDownload(
        BlobContainerClient blobContainerClient,
        IEnumerable<(string ComponentName, string? ContentHash)> components)
    {
        var blobs = ListBlobsFlatListing(blobContainerClient)
            .OrderByDescending(x => x.BlobName, StringComparer.Ordinal)
            .ToArray();

        if (blobs.Length == 0)
        {
            return new List<string>();
        }

        var blobsToDownload = new List<string>();

        foreach (var (componentName, contentHash) in components)
        {
            var latestReleasedBlobName = blobs
                .FirstOrDefault(x => x.BlobName.Contains(
                    componentName,
                    StringComparison.OrdinalIgnoreCase));

            if (latestReleasedBlobName is not (null, null) &&
                (contentHash is null || (latestReleasedBlobName.ContentHash is not null &&
                                         contentHash != ConvertBase64ToHex(latestReleasedBlobName.ContentHash))) &&
                !blobsToDownload.Contains(latestReleasedBlobName.BlobName!, StringComparer.Ordinal))
            {
                blobsToDownload.Add(latestReleasedBlobName.BlobName!);
            }
        }

        return blobsToDownload;
    }

    private static async Task<List<FileInfo>> HandleFileDownloads(
        string downloadFolder,
        IEnumerable<string> blobPaths,
        BlobContainerClient blobContainerClient)
    {
        var downloadedFiles = new List<FileInfo>();
        foreach (var blobPath in blobPaths)
        {
            var blobClient = blobContainerClient.GetBlobClient(blobPath);
            var fileName = blobClient.Name.Split('/')[^1];
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            var downloadFileForComponent = Path.Combine(downloadFolder, fileName);

            await blobClient
                .DownloadToAsync(downloadFileForComponent)
                .ConfigureAwait(true);

            var file = new FileInfo(Path.Combine(downloadFolder, fileName));
            if (file is { Exists: true, Length: > 0 })
            {
                downloadedFiles.Add(file);
            }
        }

        return downloadedFiles;
    }

    private static IEnumerable<(string BlobName, string? ContentHash)> ListBlobsFlatListing(
        BlobContainerClient blobContainerClient)
        => GetBlobNamesWithHash(GetBlobsAsPages(blobContainerClient));

    private static IEnumerable<Page<BlobItem>> GetBlobsAsPages(
        BlobContainerClient blobContainerClient)
        => blobContainerClient
            .GetBlobs()
            .AsPages(default, 100);

    private static IEnumerable<(string BlobName, string? ContentHash)> GetBlobNamesWithHash(
        IEnumerable<Page<BlobItem>> blobPages)
    {
        var blobNamesWithHash = new List<(string, string?)>();

        foreach (var blobPage in blobPages)
        {
            foreach (var blobItem in blobPage.Values)
            {
                var contentHash = blobItem.Properties.ContentHash;
                var hashString = contentHash is null
                    ? null
                    : Convert.ToBase64String(contentHash);

                blobNamesWithHash.Add((blobItem.Name, hashString));
            }
        }

        return blobNamesWithHash;
    }

    public static string ConvertBase64ToHex(
        string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        return Convert.ToHexString(bytes);
    }
}