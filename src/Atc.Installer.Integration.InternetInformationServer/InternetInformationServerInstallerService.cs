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

    public bool IsNodeJs18()
        => InstalledAppsInstallerService.Instance.IsNodeJs18();

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
        ArgumentNullException.ThrowIfNull(applicationPoolName);

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
        ArgumentNullException.ThrowIfNull(websiteName);

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

    public Task<bool> CreateApplicationPool(
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(applicationPoolName);

        var applicationPoolState = GetApplicationPoolState(applicationPoolName);
        if (applicationPoolState != ComponentRunningState.NotAvailable)
        {
            return Task.FromResult(false);
        }

        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools.Add(applicationPoolName);
            applicationPool.ManagedRuntimeVersion = setApplicationPoolToUseDotNetClr
                ? "v4.0"
                : "Classic";
            applicationPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            applicationPool.StartMode = StartMode.AlwaysRunning;
            serverManager.CommitChanges();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> CreateWebsite(
        string websiteName,
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        DirectoryInfo physicalPath,
        ushort port,
        string? hostName = null,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
        => CreateWebsite(
            websiteName,
            applicationPoolName,
            setApplicationPoolToUseDotNetClr,
            physicalPath,
            port,
            httpsPort: null,
            hostName,
            requireServerNameIndication: false,
            timeoutInSeconds,
            cancellationToken);

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    public async Task<bool> CreateWebsite(
        string websiteName,
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        DirectoryInfo physicalPath,
        ushort httpPort,
        ushort? httpsPort,
        string? hostName,
        bool requireServerNameIndication,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(websiteName);
        ArgumentNullException.ThrowIfNull(applicationPoolName);
        ArgumentNullException.ThrowIfNull(physicalPath);

        var websiteState = GetWebsiteState(websiteName);
        if (websiteState != ComponentRunningState.NotAvailable)
        {
            return false;
        }

        var applicationPoolState = GetApplicationPoolState(applicationPoolName);
        if (applicationPoolState == ComponentRunningState.NotAvailable)
        {
            var isApplicationPoolCreated = await CreateApplicationPool(
                applicationPoolName,
                setApplicationPoolToUseDotNetClr,
                timeoutInSeconds,
                cancellationToken).ConfigureAwait(false);

            if (!isApplicationPoolCreated)
            {
                return false;
            }
        }

        var httpBindingInformation = $"*:{httpPort}:";
        var httpsBindingInformation = $"*:{httpsPort}:";
        if (!string.IsNullOrEmpty(hostName))
        {
            httpBindingInformation += hostName;
            httpsBindingInformation += hostName;
        }

        try
        {
            if (!Directory.Exists(physicalPath.FullName))
            {
                Directory.CreateDirectory(physicalPath.FullName);
            }

            using var serverManager = new ServerManager();
            var website = serverManager.Sites.Add(
                websiteName,
                "http",
                httpBindingInformation,
                physicalPath.FullName);
            website.ApplicationDefaults.ApplicationPoolName = applicationPoolName;
            website.ServerAutoStart = false;

            if (httpsPort is not null &&
                !string.IsNullOrEmpty(hostName))
            {
                var bindingIpAddress = httpsBindingInformation.Split(':')[0];
                var bindingPort = httpsBindingInformation.Split(':')[1];
                var newBinding = website.Bindings.Add(
                    $"{bindingIpAddress}:{bindingPort}:{hostName}",
                    "https");
                newBinding.SslFlags = requireServerNameIndication
                    ? SslFlags.Sni
                    : SslFlags.None;
            }

            serverManager.CommitChanges();
            return true;
        }
        catch
        {
            return false;
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
        ArgumentException.ThrowIfNullOrEmpty(websiteName);

        try
        {
            using var serverManager = new ServerManager();
            var website = serverManager.Sites[websiteName];
            if (website?.State is not (ObjectState.Started or ObjectState.Starting))
            {
                return Task.FromResult(false);
            }

            website.Stop();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (website is not { State: ObjectState.Stopped } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = website.State == ObjectState.Stopped;
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
        ArgumentException.ThrowIfNullOrEmpty(applicationPoolName);

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
        ArgumentException.ThrowIfNullOrEmpty(websiteName);

        try
        {
            using var serverManager = new ServerManager();
            var website = serverManager.Sites[websiteName];
            if (website?.State is not (ObjectState.Stopped or ObjectState.Stopping))
            {
                return Task.FromResult(false);
            }

            website.Start();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (website is not { State: ObjectState.Started } &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                totalSecondsElapsed++;
            }

            var result = website.State == ObjectState.Started;
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