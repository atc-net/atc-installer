namespace Atc.Installer.Integration.PostgreSql;

public interface IPostgreSqlServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetRootPath();

    FileInfo? GetInstalledMainFile();
}