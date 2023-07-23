// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IInternetInformationServerInstallerService iisInstallerService;

    public InternetInformationServerComponentProviderViewModel(
        IInternetInformationServerInstallerService internetInformationServerInstallerService,
        INetworkShellService networkShellService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            networkShellService,
            refComponentProviders,
            installerTempDirectory,
            installationDirectory,
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.iisInstallerService = internetInformationServerInstallerService ?? throw new ArgumentNullException(nameof(internetInformationServerInstallerService));

        InitializeFromApplicationOptions(applicationOption);
    }

    public bool IsRequiredWebSockets { get; private set; }

    public string? HostName { get; private set; }

    public ushort? HttpPort { get; private set; }

    public ushort? HttpsPort { get; private set; }

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        InstallationPrerequisites.SuppressOnChangedNotification = true;

        if (iisInstallerService.IsInstalled)
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Information, "IIS is installed");
            if (iisInstallerService.IsRunning)
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Information, "IIS is running");
            }
            else
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Error, "IIS is not running");
            }

            CheckPrerequisitesForInstalled();
            CheckPrerequisitesForHostingFramework();
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Error, "IIS is not installed");
        }

        InstallationPrerequisites.SuppressOnChangedNotification = false;
        RaisePropertyChanged(nameof(InstallationPrerequisites));
    }

    [SuppressMessage("Unknown", "S3440:RemoveThisUselessCondition", Justification = "OK.")]
    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!iisInstallerService.IsInstalled)
        {
            return;
        }

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.PartiallyRunning &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        var applicationPoolState = iisInstallerService.GetApplicationPoolState(Name);
        var websiteState = iisInstallerService.GetWebsiteState(Name);

        RunningState = applicationPoolState switch
        {
            ComponentRunningState.Stopped when websiteState == ComponentRunningState.Stopped => ComponentRunningState.Stopped,
            ComponentRunningState.Running when websiteState == ComponentRunningState.Running => ComponentRunningState.Running,
            _ => applicationPoolState == ComponentRunningState.Running || websiteState == ComponentRunningState.Running
                ? ComponentRunningState.PartiallyRunning
                : ComponentRunningState.NotAvailable,
        };

        if (RunningState == ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    public override void UpdateConfigurationDynamicJson(
        string fileName,
        DynamicJson dynamicJson)
    {
        ArgumentNullException.ThrowIfNull(dynamicJson);

        foreach (var configurationSettingsFile in ConfigurationSettingsFiles.JsonItems)
        {
            if (!configurationSettingsFile.FileName.Equals(fileName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var setting in configurationSettingsFile.Settings)
            {
                var value = setting.Value;
                if (value is JsonElement { ValueKind: JsonValueKind.String } jsonElement)
                {
                    value = ResolveTemplateIfNeededByApplicationSettingsLookup(jsonElement.GetString()!);
                }

                dynamicJson.SetValue(setting.Key, value);
            }
        }
    }

    public override void UpdateConfigurationXmlDocument(
        string fileName,
        XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        foreach (var configurationSettingsFile in ConfigurationSettingsFiles.XmlItems)
        {
            if (!configurationSettingsFile.FileName.Equals(fileName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var setting in configurationSettingsFile.Settings)
            {
                if (setting.Element.Equals("add", StringComparison.Ordinal) &&
                    setting.Path.EndsWith("appSettings", StringComparison.Ordinal))
                {
                    var attributeKey = setting.Attributes.FirstOrDefault(x => x.Key == "key");
                    var attributeValue = setting.Attributes.FirstOrDefault(x => x.Key == "value");
                    if (!string.IsNullOrEmpty(attributeKey?.Value?.ToString()) &&
                        !string.IsNullOrEmpty(attributeValue?.Value?.ToString()))
                    {
                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(attributeValue.Value.ToString()!);
                        xmlDocument.SetAppSettings(attributeKey.Value.ToString()!, value);
                    }
                }
                else
                {
                    foreach (var settingAttribute in setting.Attributes)
                    {
                        if (settingAttribute.Value is null)
                        {
                            continue;
                        }

                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(settingAttribute.Value.ToString()!);
                        xmlDocument.SetValue(setting.Path, setting.Element, settingAttribute.Key, value);
                    }
                }
            }
        }
    }

    public override string ResolvedVirtualRootFolder(
        string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);

        if (InstallationFolderPath is not null)
        {
            folder = folder.Replace(".", InstallationFolderPath, StringComparison.Ordinal);
        }

        return folder;
    }

    public override bool CanServiceStopCommandHandler()
        => RunningState is ComponentRunningState.Running or ComponentRunningState.PartiallyRunning;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Stop service"));

        var isStopped = false;
        if (RunningState == ComponentRunningState.PartiallyRunning)
        {
            var websiteState = iisInstallerService.GetWebsiteState(Name);
            if (websiteState == ComponentRunningState.Running)
            {
                isStopped = await iisInstallerService
                    .StopWebsite(Name)
                    .ConfigureAwait(true);
            }

            var applicationPoolState = iisInstallerService.GetApplicationPoolState(Name);
            if (applicationPoolState == ComponentRunningState.Running)
            {
                isStopped = await iisInstallerService
                    .StopApplicationPool(Name)
                    .ConfigureAwait(true);
            }
        }
        else
        {
            isStopped = await iisInstallerService
                .StopWebsiteApplicationPool(Name, Name)
                .ConfigureAwait(true);
        }

        if (isStopped)
        {
            RunningState = ComponentRunningState.Stopped;
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Service is stopped");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Could not stop service");
        }

        IsBusy = false;
    }

    public override bool CanServiceStartCommandHandler()
        => RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Start"));

        var isStarted = await iisInstallerService
            .StartWebsiteAndApplicationPool(Name, Name)
            .ConfigureAwait(true);

        if (isStarted)
        {
            RunningState = ComponentRunningState.Running;
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Service is started");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Could not start service");
        }

        IsBusy = false;
    }

    public override bool CanServiceDeployCommandHandler()
    {
        if (UnpackedZipFolderPath is null)
        {
            return false;
        }

        return RunningState switch
        {
            ComponentRunningState.Stopped => true,
            ComponentRunningState.Unknown when InstallationState is ComponentInstallationState.NotInstalled or ComponentInstallationState.InstalledWithOldVersion => true,
            ComponentRunningState.NotAvailable when InstallationState is ComponentInstallationState.NotInstalled or ComponentInstallationState.InstalledWithOldVersion => true,
            _ => false,
        };
    }

    public override Task ServiceDeployCommandHandler()
        => ServiceDeployAndStart(useAutoStart: false);

    public override Task ServiceDeployAndStartCommandHandler()
        => ServiceDeployAndStart(useAutoStart: true);

    private void InitializeFromApplicationOptions(
        ApplicationOption applicationOption)
    {
        if (InstallationFolderPath is not null)
        {
            InstallationFolderPath = iisInstallerService.ResolvedVirtualRootFolder(InstallationFolderPath)!;
        }

        if (InstalledMainFilePath is not null)
        {
            InstalledMainFilePath = iisInstallerService.ResolvedVirtualRootFolder(InstalledMainFilePath);
        }

        IsRequiredWebSockets = applicationOption.DependentComponents.Contains("WebSockets", StringComparer.Ordinal);

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            HostName = hostNameValue;
        }

        if (TryGetUshortFromApplicationSettings("HttpPort", out var httpValue))
        {
            HttpPort = httpValue;
            if (!string.IsNullOrEmpty(HostName))
            {
                if (TryGetBooleanFromApplicationSettings("SwaggerEnabled", out var swaggerEnabledValue) &&
                    swaggerEnabledValue)
                {
                    Endpoints.Add(new EndpointViewModel("Http", ComponentEndpointType.BrowserLink, $"http://{HostName}:{HttpPort}/swagger"));
                }
                else
                {
                    Endpoints.Add(new EndpointViewModel("Http", ComponentEndpointType.BrowserLink, $"http://{HostName}:{HttpPort}"));
                }
            }
        }

        if (TryGetUshortFromApplicationSettings("HttpsPort", out var httpsValue))
        {
            HttpsPort = httpsValue;
            if (!string.IsNullOrEmpty(HostName))
            {
                if (TryGetBooleanFromApplicationSettings("SwaggerEnabled", out var swaggerEnabledValue) &&
                    swaggerEnabledValue)
                {
                    Endpoints.Add(new EndpointViewModel("Https", ComponentEndpointType.BrowserLink, $"https://{HostName}:{HttpsPort}/swagger"));
                }
                else
                {
                    Endpoints.Add(new EndpointViewModel("Https", ComponentEndpointType.BrowserLink, $"https://{HostName}:{HttpsPort}"));
                }
            }
        }

        foreach (var endpoint in applicationOption.Endpoints)
        {
            Endpoints.Add(
                new EndpointViewModel(
                    endpoint.Name,
                    endpoint.EndpointType,
                    ResolveTemplateIfNeededByApplicationSettingsLookup(endpoint.Endpoint)));
        }
    }

    private async Task ServiceDeployAndStart(
        bool useAutoStart)
    {
        if (!CanServiceDeployCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Deploy"));

        var isDone = false;

        if (InstallationState == ComponentInstallationState.NotInstalled &&
            UnpackedZipFolderPath is not null &&
            InstallationFolderPath is not null &&
            HttpPort.HasValue)
        {
            isDone = await ServiceDeployWebsiteCreate(useAutoStart).ConfigureAwait(true);
        }
        else if (RunningState == ComponentRunningState.Stopped &&
                 UnpackedZipFolderPath is not null &&
                 InstallationFolderPath is not null)
        {
            isDone = await ServiceDeployWebsiteUpdate(useAutoStart).ConfigureAwait(true);
        }

        if (isDone)
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Deployed");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Not Deployed");
        }

        IsBusy = false;
    }

    private void CheckPrerequisitesForInstalled()
    {
        if (iisInstallerService.GetWwwRootPath() is null)
        {
            AddToInstallationPrerequisites("WwwRootPath", LogCategoryType.Error, "IIS wwwroot path is not found");
        }

        if (iisInstallerService.IsInstalledManagementConsole())
        {
            AddToInstallationPrerequisites("IsInstalledManagementConsole", LogCategoryType.Information, "IIS Management Console is installed");
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalledManagementConsole", LogCategoryType.Error, "IIS Management Console is not installed");
        }

        if (IsRequiredWebSockets)
        {
            if (iisInstallerService.IsComponentInstalledWebSockets())
            {
                AddToInstallationPrerequisites("IsComponentInstalledWebSockets", LogCategoryType.Information, "IIS WebSockets is installed");
            }
            else
            {
                AddToInstallationPrerequisites("IsComponentInstalledWebSockets", LogCategoryType.Warning, "IIS WebSockets is not installed");
            }
        }
    }

    private void CheckPrerequisitesForHostingFramework()
    {
        switch (HostingFramework)
        {
            case HostingFrameworkType.DonNetFramework48:
                if (iisInstallerService.IsMicrosoftDonNetFramework48())
                {
                    AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Information, "Microsoft .NET Framework 4.8 is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Warning, "Microsoft .NET Framework 4.8 is not installed");
                }

                break;

            case HostingFrameworkType.DotNet7:
                if (iisInstallerService.IsComponentInstalledMicrosoftNetAppHostPack7())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetAppHostPack7", LogCategoryType.Information, "IIS module 'Microsoft .NET AppHost Pack - 7' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetAppHostPack7", LogCategoryType.Warning, "IIS module 'Microsoft .NET AppHost Pack - 7' is not installed");
                }

                if (iisInstallerService.IsComponentInstalledMicrosoftAspNetCoreModule2())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Information, "IIS module 'Microsoft ASP.NET Core Module V2' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Warning, "IIS module 'Microsoft ASP.NET Core Module V2' is not installed");
                }

                break;

            case HostingFrameworkType.NodeJs:
                if (iisInstallerService.IsComponentInstalledUrlRewriteModule2())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Information, "IIS module 'URL Rewrite Module 2' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Warning, "IIS module 'URL Rewrite Module 2' is not installed");
                }

                if (iisInstallerService.IsNodeJs18())
                {
                    AddToInstallationPrerequisites("IsNodeJs", LogCategoryType.Information, "NodeJS is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsNodeJs", LogCategoryType.Warning, "NodeJS is not installed");
                }

                break;
            default:
                throw new SwitchCaseDefaultException(HostingFramework);
        }
    }

    private void AddToInstallationPrerequisites(
        string key,
        LogCategoryType categoryType,
        string message)
    {
        InstallationPrerequisites.Add(
            new InstallationPrerequisiteViewModel(
                $"IIS_{key}",
                categoryType,
                message));
    }

    private async Task<bool> ServiceDeployWebsiteCreate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null ||
            HttpPort is null)
        {
            return isDone;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Create Website"));

        var isWebsiteCreated = await iisInstallerService
            .CreateWebsite(
                Name,
                Name,
                setApplicationPoolToUseDotNetClr: true,
                new DirectoryInfo(InstallationFolderPath),
                HttpPort.Value,
                HttpsPort,
                HostName,
                requireServerNameIndication: true)
            .ConfigureAwait(true);

        if (isWebsiteCreated)
        {
            LogItems.Add(LogItemFactory.CreateInformation("Website is created"));

            await iisInstallerService
                .StopApplicationPool(Name)
                .ConfigureAwait(true);

            await ServiceDeployWebsitePostProcessing(useAutoStart).ConfigureAwait(true);

            isDone = true;
        }
        else
        {
            LogItems.Add(LogItemFactory.CreateError("Website is not created"));
        }

        return isDone;
    }

    private async Task<bool> ServiceDeployWebsiteUpdate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        await ServiceDeployWebsitePostProcessing(useAutoStart).ConfigureAwait(true);

        isDone = true;

        return isDone;
    }

    private async Task ServiceDeployWebsitePostProcessing(
        bool useAutoStart)
    {
        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        if (HostingFramework == HostingFrameworkType.NodeJs)
        {
            await iisInstallerService
                .UnlockConfigSectionSystemWebServerModules()
                .ConfigureAwait(true);

            await iisInstallerService
                .EnsureSettingsForComponentUrlRewriteModule2(new DirectoryInfo(InstallationFolderPath!))
                .ConfigureAwait(true);
        }

        if (HttpPort.HasValue)
        {
            await EnsureUrlReservationEntryIfNeeded("http", HttpPort.Value)
                .ConfigureAwait(true);
        }

        if (HttpsPort.HasValue)
        {
            await EnsureUrlReservationEntryIfNeeded("https", HttpsPort.Value)
                .ConfigureAwait(true);
        }

        InstallationState = ComponentInstallationState.Installed;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting website"));
            await ServiceDeployWebsiteStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Website is started")
                : LogItemFactory.CreateWarning("Website is not started"));
        }
        else
        {
            await iisInstallerService
                .StopApplicationPool(Name)
                .ConfigureAwait(true);
        }

        WorkOnAnalyzeAndUpdateStatesForVersion();
    }

    private async Task ServiceDeployWebsiteStart()
    {
        await Task
            .Delay(100)
            .ConfigureAwait(false);

        RunningState = iisInstallerService.GetWebsiteState(Name);

        if (RunningState == ComponentRunningState.Stopped)
        {
            var isWebsiteStarted = await iisInstallerService
                .StartWebsite(Name)
                .ConfigureAwait(true);

            if (!isWebsiteStarted)
            {
                LogItems.Add(LogItemFactory.CreateWarning("Website have some problem with startup"));
            }

            RunningState = iisInstallerService.GetWebsiteState(Name);
        }
    }
}