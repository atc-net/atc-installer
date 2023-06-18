namespace Atc.Installer.Integration.ElasticSearch;

public interface IElasticSearchServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetRootPath();

    FileInfo? GetInstalledMainFile();

    string GetServiceName();

    bool IsComponentInstalledJava();

    Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string webProtocol,
        string hostName,
        ushort hostPort,
        string username,
        string password);
}