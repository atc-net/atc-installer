// ReSharper disable ConvertIfStatementToReturnStatement
namespace Atc.Installer.Integration.PostgreSql;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "OK.")]
[SuppressMessage("Major Code Smell", "S3010:Static fields should not be updated in constructors", Justification = "OK.")]
public sealed class PostgreSqlServerInstallerService : IPostgreSqlServerInstallerService
{
    private static IWindowsApplicationInstallerService? waInstanceService;
    private static IInstalledAppsInstallerService? iaInstanceService;

    public PostgreSqlServerInstallerService(
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        IInstalledAppsInstallerService installedAppsInstallerService)
    {
        waInstanceService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        iaInstanceService = installedAppsInstallerService ?? throw new ArgumentNullException(nameof(installedAppsInstallerService));
    }

    public bool IsInstalled
    {
        get
        {
            var installationFolderFor64Bit = GetRootPath();
            return installationFolderFor64Bit is not null;
        }
    }

    public bool IsRunning
    {
        get
        {
            var runningState = waInstanceService!.GetServiceState(GetServiceName());
            return runningState == ComponentRunningState.Running;
        }
    }

    public DirectoryInfo? GetRootPath()
    {
        var installedMainFile = GetInstalledMainFile();
        return installedMainFile is null
            ? null
            : new FileInfo(installedMainFile.FullName).Directory!.Parent;
    }

    public FileInfo? GetInstalledMainFile()
    {
        try
        {
            var directoryRoot = LocateRootFolder();

            return directoryRoot?.SearchForFile("pg_ctl.exe");
        }
        catch
        {
            return null;
        }
    }

    public string GetServiceName()
        => $"postgresql-x64-{GetMainVersion()}";

    private int? GetMainVersion()
    {
        var root = GetRootPath();
        if (root is null)
        {
            return null;
        }

        var version = iaInstanceService!.GetAppInstalledVersionByDisplayName("PostgreSQL");
        return version?.Major;
    }

    public Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string hostName,
        ushort hostPort,
        string database,
        string username,
        string password)
        => TestConnection($"Host={hostName}:{hostPort};Database={database};Username={username};Password={password};");

    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "OK - not possible to do properly.")]
    [SuppressMessage("Usage", "MA0004:Use Task.ConfigureAwait(false)", Justification = "OK - not possible to do properly.")]
    public async Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection
                .OpenAsync()
                .ConfigureAwait(false);

            return (IsSucceeded: true, ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return (IsSucceeded: false, ErrorMessage: ex.Message);
        }
    }

    private static DirectoryInfo? LocateRootFolder()
    {
        var directoryRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL"));
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        directoryRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PostgreSQL"));
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        return null;
    }
}