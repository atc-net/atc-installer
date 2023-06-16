namespace Atc.Installer.Integration.PostgreSql;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Major Code Smell", "S3010:Static fields should not be updated in constructors", Justification = "OK.")]
public sealed class PostgreSqlServerInstallerService : IPostgreSqlServerInstallerService
{
    private static readonly object InstanceLock = new();
    private static PostgreSqlServerInstallerService? instance;
    private static WindowsApplicationInstallerService? waInstanceService;
    private static InstalledAppsInstallerService? iaInstanceService;

    private PostgreSqlServerInstallerService()
    {
        waInstanceService = WindowsApplicationInstallerService.Instance;
        iaInstanceService = InstalledAppsInstallerService.Instance;
    }

    public static PostgreSqlServerInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new PostgreSqlServerInstallerService();
            }
        }
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
            var runningState = waInstanceService!.GetServiceState($"postgresql-x64-{GetMainVersion()}");
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
            const string rootFolder = @"C:\Program Files\PostgreSQL";
            if (!Directory.Exists(rootFolder))
            {
                return null;
            }

            var locations = Directory
                .GetFiles(rootFolder, "pg_ctl.exe", SearchOption.AllDirectories)
                .OrderByDescending(x => x, StringComparer.Ordinal)
                .ToArray();

            return locations.Length > 0
                ? new FileInfo(locations[0])
                : null;
        }
        catch
        {
            return null;
        }
    }

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
}