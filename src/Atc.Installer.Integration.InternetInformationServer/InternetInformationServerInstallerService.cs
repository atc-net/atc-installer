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

    public bool IsMicrosoftDonNetFramework48()
        => InstalledAppsInstallerService.Instance.IsMicrosoftDonNetFramework48();

    public bool IsMicrosoftDonNet7()
        => InstalledAppsInstallerService.Instance.IsMicrosoftDonNet7();

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
            return registryKeyValue is not null &&
                   (int)registryKeyValue == 1;
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

    public ComponentRunningState GetApplicationPoolState(
        string applicationPoolName)
    {
        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools[applicationPoolName];
            if (applicationPool is null)
            {
                return ComponentRunningState.NotAvailable;
            }

            return applicationPool.State switch
            {
                ObjectState.Starting => ComponentRunningState.Checking,
                ObjectState.Started => ComponentRunningState.Running,
                ObjectState.Stopping => ComponentRunningState.Checking,
                ObjectState.Stopped => ComponentRunningState.Stopped,
                ObjectState.Unknown => ComponentRunningState.Unknown,
                _ => throw new SwitchExpressionException(applicationPool.State),
            };
        }
        catch
        {
            return ComponentRunningState.Unknown;
        }
    }

    public ComponentRunningState GetWebsiteState(
        string websiteName)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[websiteName];
            if (site is null)
            {
                return ComponentRunningState.NotAvailable;
            }

            return site.State switch
            {
                ObjectState.Starting => ComponentRunningState.Checking,
                ObjectState.Started => ComponentRunningState.Running,
                ObjectState.Stopping => ComponentRunningState.Checking,
                ObjectState.Stopped => ComponentRunningState.Stopped,
                ObjectState.Unknown => ComponentRunningState.Unknown,
                _ => throw new SwitchExpressionException(site.State),
            };
        }
        catch
        {
            return ComponentRunningState.Unknown;
        }
    }

    public Task<bool> StopApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools[applicationPoolName];
            if (applicationPool?.State is not (ObjectState.Started or ObjectState.Starting))
            {
                return Task.FromResult(false);
            }

            applicationPool.Stop();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (applicationPool is not { State: ObjectState.Stopped } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = applicationPool.State == ObjectState.Stopped;
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> StopWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[websiteName];
            if (site?.State is not (ObjectState.Started or ObjectState.Starting))
            {
                return Task.FromResult(false);
            }

            site.Stop();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (site is not { State: ObjectState.Stopped } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = site.State == ObjectState.Stopped;
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> StopWebsiteApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
        => await StopApplicationPool(applicationPoolName, timeoutInSeconds, cancellationToken).ConfigureAwait(false) &&
           await StopWebsite(websiteName, timeoutInSeconds, cancellationToken).ConfigureAwait(false);

    public Task<bool> StartApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools[applicationPoolName];
            if (applicationPool?.State is not (ObjectState.Stopped or ObjectState.Stopping))
            {
                return Task.FromResult(false);
            }

            applicationPool.Start();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (applicationPool is not { State: ObjectState.Started } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = applicationPool.State == ObjectState.Started;
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> StartWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[websiteName];
            if (site?.State is not (ObjectState.Stopped or ObjectState.Stopping))
            {
                return Task.FromResult(false);
            }

            site.Start();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (site is not { State: ObjectState.Started } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = site.State == ObjectState.Started;
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> StartWebsiteAndApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
        => await StartApplicationPool(applicationPoolName, timeoutInSeconds, cancellationToken).ConfigureAwait(false) &&
           await StartWebsite(websiteName, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
}