// ReSharper disable StringLiteralTypo
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
    private readonly FileInfo appCmdFile = new(@"C:\Windows\System32\inetsrv\appcmd.exe");

    private readonly IInstalledAppsInstallerService iaInstallerService;

    public InternetInformationServerInstallerService(
        IInstalledAppsInstallerService installedAppsInstallerService)
    {
        this.iaInstallerService = installedAppsInstallerService ?? throw new ArgumentNullException(nameof(installedAppsInstallerService));
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
        => iaInstallerService.IsMicrosoftDonNetFramework48();

    public bool IsMicrosoftDonNet7()
        => iaInstallerService.IsMicrosoftDonNet7();

    public bool IsNodeJs18()
        => TaskHelper.RunSync(() => iaInstallerService.IsNodeJs18());

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

    public bool IsComponentInstalledMicrosoftNetHost7()
        => iaInstallerService.IsAppInstalledByDisplayName("Microsoft .NET Host - 7.");

    public bool IsComponentInstalledMicrosoftAspNetCoreModule2()
    {
        if (!iaInstallerService.IsAppInstalledByDisplayName("Microsoft ASP.NET Core Module V2"))
        {
            return false;
        }

        var applicationHostConfigFile = Environment.ExpandEnvironmentVariables(@"%windir%\System32\inetsrv\config\applicationHost.config");

        if (!File.Exists(applicationHostConfigFile))
        {
            return false;
        }

        var doc = XDocument.Load(applicationHostConfigFile);
        if (doc.Root is null)
        {
            return false;
        }

        var moduleElement = doc.Root
            .Element("system.webServer")!
            .Element("globalModules")!
            .Elements("add")
            .FirstOrDefault(e =>
                (string)e.Attribute("name")! == "AspNetCoreModuleV2" ||
                (string)e.Attribute("name")! == "AspNetCoreModule");

        return moduleElement != null;
    }

    public bool IsComponentInstalledUrlRewriteModule2()
    {
        if (!iaInstallerService.IsAppInstalledByDisplayName("IIS URL Rewrite Module 2"))
        {
            return false;
        }

        var applicationHostConfigFile = Environment.ExpandEnvironmentVariables(@"%windir%\System32\inetsrv\config\applicationHost.config");

        if (!File.Exists(applicationHostConfigFile))
        {
            return false;
        }

        var doc = XDocument.Load(applicationHostConfigFile);
        if (doc.Root is null)
        {
            return false;
        }

        var moduleElement = doc.Root
            .Element("system.webServer")!
            .Element("globalModules")!
            .Elements("add")
            .FirstOrDefault(e => (string)e.Attribute("name")! == "RewriteModule");

        return moduleElement != null;
    }

    public string? ResolvedVirtualRootFolder(
        string folder)
        => folder is not null &&
           folder.StartsWith(@".\", StringComparison.Ordinal) &&
           IsInstalled &&
           GetWwwRootPath() is not null
            ? folder.Replace(@".\", GetWwwRootPath()!.FullName + @"\", StringComparison.Ordinal)
            : folder;

    public async Task<(bool IsSucceeded, string? ErrorMessage)> UnlockConfigSectionSystemWebServerModules()
    {
        var (isSuccessful, output) = await ProcessHelper
            .Execute(
                appCmdFile,
                arguments: "unlock config -section:\"system.webServer/modules\"")
            .ConfigureAwait(false);

        if (isSuccessful && output is not null)
        {
            return (IsSucceeded: false, ErrorMessage: null);
        }

        if (!string.IsNullOrEmpty(output))
        {
            output = output.IndentEachLineWith("    ");
        }

        return (IsSucceeded: false, ErrorMessage: $"Unlock config -section:\"system.webServer/modules\"{Environment.NewLine}{output}");
    }

    public async Task<(bool IsSucceeded, string? ErrorMessage)> EnsureSettingsForComponentUrlRewriteModule2(
        DirectoryInfo installationPath)
    {
        ArgumentNullException.ThrowIfNull(installationPath);

        var webConfigFile = new FileInfo(Path.Combine(installationPath.FullName, "web.config"));

        if (webConfigFile.Exists)
        {
            // TODO: Ensure xml contains rewrite-section
            return (IsSucceeded: false, ErrorMessage: "Not implemented yet");
        }

        var defaultXml = GetResourceDefaultNodeJsUrlWebConfig()!;

        await FileHelper
            .WriteAllTextAsync(webConfigFile, defaultXml)
            .ConfigureAwait(false);

        return (IsSucceeded: true, ErrorMessage: null);
    }

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

    public async Task<bool> StopApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationPoolName);

        var applicationPoolState = GetApplicationPoolState(applicationPoolName);
        if (applicationPoolState is not ComponentRunningState.Running)
        {
            return false;
        }

        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools[applicationPoolName];
            applicationPool.Stop();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (applicationPoolState is
                       ComponentRunningState.Checking or
                       ComponentRunningState.Running &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                await Task
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(true);
                totalSecondsElapsed++;

                applicationPoolState = GetApplicationPoolState(applicationPoolName);
            }

            return applicationPoolState == ComponentRunningState.Stopped;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(websiteName);

        var websiteState = GetWebsiteState(websiteName);
        if (websiteState is not ComponentRunningState.Running)
        {
            return false;
        }

        try
        {
            using var serverManager = new ServerManager();
            var website = serverManager.Sites[websiteName];
            website.Stop();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (websiteState is
                       ComponentRunningState.Checking or
                       ComponentRunningState.Running &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                await Task
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(true);
                totalSecondsElapsed++;

                websiteState = GetWebsiteState(websiteName);
            }

            return websiteState == ComponentRunningState.Stopped;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopWebsiteApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
        => await StopWebsite(websiteName, timeoutInSeconds, cancellationToken).ConfigureAwait(false) &&
           await StopApplicationPool(applicationPoolName, timeoutInSeconds, cancellationToken).ConfigureAwait(false);

    public async Task<bool> StartApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(applicationPoolName);

        var applicationPoolState = GetApplicationPoolState(applicationPoolName);
        if (applicationPoolState is not ComponentRunningState.Stopped)
        {
            return false;
        }

        try
        {
            using var serverManager = new ServerManager();
            var applicationPool = serverManager.ApplicationPools[applicationPoolName];
            applicationPool.Start();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (applicationPoolState is
                       ComponentRunningState.Checking or
                       ComponentRunningState.Stopped &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                await Task
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(true);
                totalSecondsElapsed++;

                applicationPoolState = GetApplicationPoolState(applicationPoolName);
            }

            return applicationPoolState == ComponentRunningState.Running;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StartWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(websiteName);

        var websiteState = GetWebsiteState(websiteName);
        if (websiteState is not ComponentRunningState.Stopped)
        {
            return false;
        }

        try
        {
            using var serverManager = new ServerManager();
            var website = serverManager.Sites[websiteName];
            website.Start();
            serverManager.CommitChanges();

            var totalSecondsElapsed = 0;
            while (websiteState is
                       ComponentRunningState.Checking or
                       ComponentRunningState.Stopped &&
                   totalSecondsElapsed < timeoutInSeconds &&
                   !cancellationToken.IsCancellationRequested)
            {
                await Task
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ConfigureAwait(true);
                totalSecondsElapsed++;

                websiteState = GetWebsiteState(websiteName);
            }

            return websiteState == ComponentRunningState.Running;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StartWebsiteAndApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default)
        => await StartApplicationPool(applicationPoolName, timeoutInSeconds, cancellationToken).ConfigureAwait(false) &&
           await StartWebsite(websiteName, timeoutInSeconds, cancellationToken).ConfigureAwait(false);

    private string? GetResourceDefaultNodeJsUrlWebConfig()
        => GetResourceTextFile("default-nodejs-urlrewrite-webconfig");

    private string? GetResourceTextFile(
        string resourceName)
    {
        using var stream = this.GetType()
            .Assembly
            .GetManifestResourceStream("Atc.Installer.Integration.InternetInformationServer.Resources." + resourceName);

        if (stream is null)
        {
            return null;
        }

        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }
}