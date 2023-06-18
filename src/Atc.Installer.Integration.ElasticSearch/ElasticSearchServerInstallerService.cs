// ReSharper disable StringLiteralTypo
// ReSharper disable ConvertIfStatementToReturnStatement
namespace Atc.Installer.Integration.ElasticSearch;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Major Code Smell", "S3010:Static fields should not be updated in constructors", Justification = "OK.")]
public class ElasticSearchServerInstallerService : IElasticSearchServerInstallerService
{
    private static IWindowsApplicationInstallerService? waInstanceService;
    private static IInstalledAppsInstallerService? iaInstanceService;

    public ElasticSearchServerInstallerService(
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

            return directoryRoot?.SearchForFile(
                searchPattern: "elasticsearch-service-x64.exe",
                useRecursive: true,
                excludeDirectories: new List<string>
                {
                    "Users",
                    "Windows",
                });
        }
        catch
        {
            return null;
        }
    }

    public string GetServiceName() => "elasticsearch-service-x64";

    public bool IsComponentInstalledJava()
        => iaInstanceService!.IsJavaRuntime8();

    public async Task<(bool IsSucceeded, string? ErrorMessage)> TestConnection(
        string webProtocol,
        string hostName,
        ushort hostPort,
        string username,
        string password)
    {
        try
        {
            // TODO: Imp. this.
            return (IsSucceeded: false, ErrorMessage: "Not implemented yet");
        }
        catch (Exception ex)
        {
            return (IsSucceeded: false, ErrorMessage: ex.Message);
        }
    }

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    private static DirectoryInfo? LocateRootFolder()
    {
        var directoryRoot = new DirectoryInfo(@"C:\ELK");
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        directoryRoot = new DirectoryInfo(@"C:\ElasticSearch");
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        directoryRoot = new DirectoryInfo(@"C:\Elastic");
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        directoryRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ElasticSearch"));
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        directoryRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ElasticSearch"));
        if (directoryRoot.Exists)
        {
            return directoryRoot;
        }

        return null;
    }
}