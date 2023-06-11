namespace Atc.Installer.Integration.PostgreSql;

public interface IPostgreSqlServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetRootPath();

    FileInfo? GetInstalledMainFile();

    Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string hostName,
        ushort hostPort,
        string database,
        string username,
        string password);

    Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string connectionString);
}