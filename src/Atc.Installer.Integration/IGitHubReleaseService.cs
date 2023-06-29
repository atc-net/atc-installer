namespace Atc.Installer.Integration;

public interface IGitHubReleaseService
{
    Task<Version?> GetLatestVersion();

    Task<Uri?> GetLatestMsiLink();

    Task<byte[]> DownloadFileByLink(Uri uri);
}