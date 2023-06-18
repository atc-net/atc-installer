namespace Atc.Installer.Integration.ElasticSearch;

public interface IElasticSearchServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetRootPath();

    FileInfo? GetInstalledMainFile();

    string GetServiceName();

    bool IsComponentInstalledJava();
}