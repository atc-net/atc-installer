namespace Atc.Installer.Integration.InternetInformationServer;

/// <summary>
/// InternetInformationServerInstallerService.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/iis/install/installing-iis-7/discover-installed-components
/// </remarks>
[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Design", "CA1416:Validate platform compatibility", Justification = "OK.")]
[SuppressMessage("Minor Code Smell", "S1075: Hard coded URI ", Justification = "OK.")]
public sealed class InternetInformationServerInstallerService : IInternetInformationServerInstallerService
{
    private const string IisComponentsRegistryPath = @"SOFTWARE\Microsoft\InetStp\Components";
    private static readonly object InstanceLock = new();
    private static InternetInformationServerInstallerService? instance;

    private InternetInformationServerInstallerService()
    {
    }

    public static InternetInformationServerInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new InternetInformationServerInstallerService();
            }
        }
    }

    public bool IsInstalled
    {
        get
        {
            try
            {
                using var serverManager = new ServerManager();
                _ = serverManager.ApplicationPools;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            try
            {
                using var iisService = new ServiceController("W3SVC");
                var status = iisService.Status;
                return status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }
    }

    public DirectoryInfo? GetWwwRootPath()
    {
        var wwwRootPath = new DirectoryInfo(@"C:\inetpub\wwwroot");
        return wwwRootPath.Exists
            ? wwwRootPath
            : null;
    }

    public bool IsInstalledManagementConsole()
    {
        try
        {
            using var registryKey = Registry.LocalMachine.OpenSubKey(IisComponentsRegistryPath);
            var registryKeyValue = registryKey?.GetValue("ManagementConsole");
            return registryKeyValue is not null &&
                   (int)registryKeyValue == 1;
        }
        catch
        {
            return false;
        }
    }

    public bool IsComponentInstalledWebSockets()
    {
        try
        {
            using var registryKey = Registry.LocalMachine.OpenSubKey(IisComponentsRegistryPath);
            var registryKeyValue = registryKey?.GetValue("WebSockets");
            return registryKeyValue is not null && (int)registryKeyValue == 1;
        }
        catch
        {
            return false;
        }
    }

    public bool IsComponentInstalledMicrosoftNetAppHostPack7()
        => InstalledAppsInstallerService.Instance.IsAppInstalledByDisplayName("Microsoft .NET AppHost Pack - 7.");

    public bool IsComponentInstalledUrlRewriteModule2()
        => InstalledAppsInstallerService.Instance.IsAppInstalledByDisplayName("IIS URL Rewrite Module 2");

    public string? ResolvedVirtuelRootPath(
        string path)
        => path is not null &&
           path.StartsWith(@".\", StringComparison.Ordinal) &&
           IsInstalled &&
           GetWwwRootPath() is not null
            ? path.Replace(@".\", GetWwwRootPath()!.FullName + @"\", StringComparison.Ordinal)
            : path;
}