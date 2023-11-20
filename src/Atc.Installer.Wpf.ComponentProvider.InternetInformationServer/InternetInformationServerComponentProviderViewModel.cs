// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IInternetInformationServerInstallerService iisInstallerService;
    private X509Certificate2? x509Certificate;
    private bool enableEditingMode;

    public InternetInformationServerComponentProviderViewModel(
        ILogger<ComponentProviderViewModel> logger,
        IInternetInformationServerInstallerService internetInformationServerInstallerService,
        INetworkShellService networkShellService,
        IWindowsFirewallService windowsFirewallService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        ObservableCollectionEx<KeyValueTemplateItemViewModel> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            logger,
            networkShellService,
            windowsFirewallService,
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

        Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);
    }

    public bool IsRequiredWebSockets { get; private set; }

    public string? HostName { get; private set; }

    public ushort? HttpPort { get; private set; }

    public ushort? HttpsPort { get; private set; }

    public X509Certificate2? X509Certificate
    {
        get => x509Certificate;
        private set
        {
            x509Certificate = value;
            RaisePropertyChanged();
        }
    }

    public bool EnableEditingMode
    {
        get => enableEditingMode;
        set
        {
            if (enableEditingMode == value)
            {
                return;
            }

            enableEditingMode = value;
            RaisePropertyChanged();
        }
    }

    public IRelayCommand EditX509CertificateCommand
        => new RelayCommand(
            EditX509CertificateCommandHandler,
            CanEditX509CertificateCommandHandler);

    public IRelayCommandAsync NewX509CertificateCommand
        => new RelayCommandAsync(
            NewX509CertificateCommandHandler,
            CanNewX509CertificateCommandHandler);

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

    public override void CheckPrerequisitesState()
    {
        base.CheckPrerequisitesState();

        X509Certificate = iisInstallerService.GetWebsiteX509Certificate(Name);
    }

    [SuppressMessage("Unknown", "S3440:RemoveThisUselessCondition", Justification = "OK.")]
    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!iisInstallerService.IsInstalled)
        {
            return;
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

        if (RunningState is ComponentRunningState.Unknown or ComponentRunningState.Checking)
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

                var (isSucceeded, errorMessage) = dynamicJson.SetValue(setting.Key, value);
                if (!isSucceeded)
                {
                    Logger.LogWarning($"UpdateConfiguration for {fileName} on key={setting.Key} - {errorMessage}");
                }
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
                    if (!string.IsNullOrEmpty(attributeKey?.GetValueAsString()) &&
                        !string.IsNullOrEmpty(attributeValue?.GetValueAsString()))
                    {
                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(attributeValue.GetValueAsString());
                        xmlDocument.SetAppSettings(attributeKey.GetValueAsString(), value);
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

                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(settingAttribute.GetValueAsString());
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
            folder = folder.Replace(".", InstallationFolderPath.GetValueAsString(), StringComparison.Ordinal);
        }

        return folder;
    }

    public override bool CanServiceStopCommandHandler()
        => !DisableInstallationActions &&
           RunningState is ComponentRunningState.Running or ComponentRunningState.PartiallyRunning;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Stop service");

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
        => !DisableInstallationActions &&
           RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Start service");

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
        if (DisableInstallationActions ||
            UnpackedZipFolderPath is null)
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

    public override bool CanServiceRemoveCommandHandler()
    {
        if (DisableInstallationActions ||
            InstallationFolderPath is null ||
            InstalledMainFilePath is null)
        {
            return false;
        }

        return RunningState switch
        {
            ComponentRunningState.Stopped => true,
            _ => false,
        };
    }

    public override async Task ServiceRemoveCommandHandler()
    {
        if (!CanServiceRemoveCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Remove");

        var isDone = false;

        if (InstallationState is
                ComponentInstallationState.Installed or
                ComponentInstallationState.InstalledWithOldVersion &&
            InstallationFolderPath is not null)
        {
            isDone = await ServiceRemoveWebsite().ConfigureAwait(true);
        }

        if (isDone)
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Removed");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Not Removed");
        }

        IsBusy = false;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private void InitializeFromApplicationOptions(
        ApplicationOption applicationOption)
    {
        if (InstallationFolderPath is not null)
        {
            var folderPath = InstallationFolderPath.GetValueAsString();
            if (folderPath.StartsWith('.'))
            {
                InstallationFolderPath.Template = folderPath;
                InstallationFolderPath.TemplateLocations = new ObservableCollectionEx<string>
                {
                    "WwwRoot",
                };

                InstallationFolderPath.Value = iisInstallerService.ResolvedVirtualRootFolder(folderPath)!;
            }
        }

        if (InstalledMainFilePath is not null)
        {
            var folderPath = InstalledMainFilePath.GetValueAsString();
            if (folderPath.StartsWith('.'))
            {
                InstalledMainFilePath.Template = folderPath;
                InstalledMainFilePath.TemplateLocations = new ObservableCollectionEx<string>
                {
                    "WwwRoot",
                };

                InstalledMainFilePath.Value = iisInstallerService.ResolvedVirtualRootFolder(folderPath)!;
            }
        }

        IsRequiredWebSockets = applicationOption.DependentComponents.Contains("WebSockets", StringComparer.Ordinal);

        var useTemplateHostName = false;
        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            HostName = hostNameValue;
            useTemplateHostName = true;
        }

        if (TryGetUshortFromApplicationSettings("HttpPort", out var httpValue))
        {
            HttpPort = httpValue;
            if (!string.IsNullOrEmpty(HostName))
            {
                if (TryGetBooleanFromApplicationSettings("SwaggerEnabled", out var swaggerEnabledValue) &&
                    swaggerEnabledValue)
                {
                    if (useTemplateHostName)
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Http - Swagger",
                                ComponentEndpointType.BrowserLink,
                                $"http://{HostName}:{HttpPort}/swagger",
                                "http://[[HostName]]:[[HttpPort]]/swagger",
                                new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                    }
                    else
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Http - Swagger",
                                ComponentEndpointType.BrowserLink,
                                $"http://{HostName}:{HttpPort}/swagger",
                                $"http://{HostName}:[[HttpPort]]/swagger",
                                new List<string> { "ApplicationSettings" }));
                    }
                }
                else
                {
                    if (useTemplateHostName)
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Http",
                                ComponentEndpointType.BrowserLink,
                                $"http://{HostName}:{HttpPort}",
                                "http://[[HostName]]:[[HttpPort]]",
                                new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                    }
                    else
                    {
                        if (useTemplateHostName)
                        {
                            Endpoints.Add(
                                new EndpointViewModel(
                                    "Http",
                                    ComponentEndpointType.BrowserLink,
                                    $"http://{HostName}:{HttpPort}",
                                    "http://[[HostName]]:[[HttpPort]]",
                                    new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                        }
                        else
                        {
                            Endpoints.Add(
                                new EndpointViewModel(
                                    "Http",
                                    ComponentEndpointType.BrowserLink,
                                    $"http://{HostName}:{HttpPort}",
                                    $"http://{HostName}:[[HttpPort]]",
                                    new List<string> { "ApplicationSettings" }));
                        }
                    }
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
                    if (useTemplateHostName)
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Https - Swagger",
                                ComponentEndpointType.BrowserLink,
                                $"https://{HostName}:{HttpsPort}/swagger",
                                "https://[[HostName]]:[[HttpsPort]]/swagger",
                                new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                    }
                    else
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Https - Swagger",
                                ComponentEndpointType.BrowserLink,
                                $"https://{HostName}:{HttpsPort}/swagger",
                                $"https://{HostName}:[[HttpsPort]]/swagger",
                                new List<string> { "ApplicationSettings" }));
                    }
                }
                else
                {
                    if (useTemplateHostName)
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Https",
                                ComponentEndpointType.BrowserLink,
                                $"https://{HostName}:{HttpsPort}",
                                "https://[[HostName]]:[[HttpsPort]]",
                                new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                    }
                    else
                    {
                        Endpoints.Add(
                            new EndpointViewModel(
                                "Http",
                                ComponentEndpointType.BrowserLink,
                                $"http://{HostName}:{HttpsPort}",
                                $"http://{HostName}:[[HttpsPort]]",
                                new List<string> { "ApplicationSettings" }));
                    }
                }
            }
        }

        foreach (var endpoint in applicationOption.Endpoints)
        {
            var endpointType = endpoint.EndpointType;
            if (endpointType == ComponentEndpointType.Unknown)
            {
                if (endpoint.Name.Equals("AssemblyInfo", StringComparison.Ordinal))
                {
                    endpointType = ComponentEndpointType.ReportingAssemblyInfo;
                }
                else if (endpoint.Name.Equals("HealthCheck", StringComparison.Ordinal))
                {
                    endpointType = ComponentEndpointType.ReportingHealthCheck;
                }
            }

            if (endpoint.Endpoint.ContainsTemplateKeyBrackets())
            {
                var (resolvedValue, templateLocations) = ResolveValueAndTemplateLocations(endpoint.Endpoint);

                if (templateLocations.Count > 0)
                {
                    Endpoints.Add(
                        new EndpointViewModel(
                            endpoint.Name,
                            endpointType,
                            resolvedValue,
                            template: endpoint.Endpoint,
                            templateLocations));
                }
            }
            else
            {
                Endpoints.Add(
                    new EndpointViewModel(
                        endpoint.Name,
                        endpointType,
                        endpoint.Endpoint,
                        template: null,
                        templateLocations: null));
            }
        }
    }

    private async Task ServiceDeployAndStart(
        bool useAutoStart)
    {
        if (!CanServiceDeployCommandHandler())
        {
            return;
        }

        await SetIsBusy(value: true, delayInMs: 500).ConfigureAwait(true);

        AddLogItem(LogLevel.Trace, "Deploy");

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

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
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
                if (iisInstallerService.IsComponentInstalledMicrosoftNetHost7())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetHost7", LogCategoryType.Information, "IIS module 'Microsoft .NET Host - 7' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetHost7", LogCategoryType.Warning, "IIS module 'Microsoft .NET Host - 7' is not installed");
                }

                if (iisInstallerService.IsComponentInstalledMicrosoftAspNetCoreModule2())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Information, "IIS module 'Microsoft ASP.NET Core Module V2' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Warning, "IIS module 'Microsoft ASP.NET Core Module V2' is not installed");
                }

                if (IsDotNetBlazorWebAssembly())
                {
                    if (iisInstallerService.IsComponentInstalledUrlRewriteModule2())
                    {
                        AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Information, "IIS module 'URL Rewrite Module 2' is installed");
                    }
                    else
                    {
                        AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Warning, "IIS module 'URL Rewrite Module 2' is not installed");
                    }
                }

                break;

            case HostingFrameworkType.DotNet8:
                if (iisInstallerService.IsComponentInstalledMicrosoftNetHost8())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetHost8", LogCategoryType.Information, "IIS module 'Microsoft .NET Host - 8' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetHost8", LogCategoryType.Warning, "IIS module 'Microsoft .NET Host - 8' is not installed");
                }

                if (iisInstallerService.IsComponentInstalledMicrosoftAspNetCoreModule2())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Information, "IIS module 'Microsoft ASP.NET Core Module V2' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftAspNetCoreModule2", LogCategoryType.Warning, "IIS module 'Microsoft ASP.NET Core Module V2' is not installed");
                }

                if (IsDotNetBlazorWebAssembly())
                {
                    if (iisInstallerService.IsComponentInstalledUrlRewriteModule2())
                    {
                        AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Information, "IIS module 'URL Rewrite Module 2' is installed");
                    }
                    else
                    {
                        AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Warning, "IIS module 'URL Rewrite Module 2' is not installed");
                    }
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

        AddLogItem(LogLevel.Trace, "Create Website");

        var isWebsiteCreated = await iisInstallerService
            .CreateWebsite(
                Name,
                Name,
                setApplicationPoolToUseDotNetClr: true,
                new DirectoryInfo(InstallationFolderPath.GetValueAsString()),
                HttpPort.Value,
                HttpsPort,
                HostName,
                requireServerNameIndication: true)
            .ConfigureAwait(true);

        if (isWebsiteCreated)
        {
            AddLogItem(LogLevel.Information, "Website is created");

            await iisInstallerService
                .StopApplicationPool(Name)
                .ConfigureAwait(true);

            await ServiceDeployWebsitePostProcessing(useAutoStart).ConfigureAwait(true);

            await HandleCertificateIfNeeded().ConfigureAwait(true);

            isDone = true;
        }
        else
        {
            AddLogItem(LogLevel.Error, "Website is not created");
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

        AddLogItem(LogLevel.Trace, "Update Website");

        BackupConfigurationFilesAndLog();

        await ServiceDeployWebsitePostProcessing(useAutoStart).ConfigureAwait(true);

        AddLogItem(LogLevel.Information, "Website is updated");

        isDone = true;

        return isDone;
    }

    private async Task ServiceDeployWebsitePostProcessing(
        bool useAutoStart)
    {
        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        EnsureFirewallRules();

        if (HostingFramework == HostingFrameworkType.NodeJs)
        {
            await iisInstallerService
                .UnlockConfigSectionSystemWebServerModules()
                .ConfigureAwait(true);

            await iisInstallerService
                .EnsureSettingsForComponentUrlRewriteModule2(new DirectoryInfo(InstallationFolderPath!.GetValueAsString()))
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
            AddLogItem(LogLevel.Trace, "Auto starting website");
            await ServiceDeployWebsiteStart().ConfigureAwait(true);
            if (RunningState == ComponentRunningState.Running)
            {
                AddLogItem(LogLevel.Information, "Website is started");
            }
            else
            {
                AddLogItem(LogLevel.Warning, "Website is not started");
            }
        }
        else
        {
            await iisInstallerService
                .StopApplicationPool(Name)
                .ConfigureAwait(true);
        }

        WorkOnAnalyzeAndUpdateStatesForVersion();
    }

    private async Task<bool> ServiceRemoveWebsite()
    {
        var isDone = false;

        if (InstalledMainFilePath is null)
        {
            return false;
        }

        AddLogItem(LogLevel.Trace, "Remove Website");

        var stop1 = true;
        var stop2 = true;
        var applicationPoolState = iisInstallerService.GetApplicationPoolState(Name);
        if (applicationPoolState == ComponentRunningState.Running)
        {
            stop1 = await iisInstallerService
                .StopApplicationPool(Name)
                .ConfigureAwait(true);
        }

        var websitePoolState = iisInstallerService.GetWebsiteState(Name);
        if (websitePoolState == ComponentRunningState.Running)
        {
            stop2 = await iisInstallerService
                .StopWebsite(Name)
                .ConfigureAwait(true);
        }

        if (stop1 && stop2)
        {
            var isWebsiteDeleted = await iisInstallerService
                .DeleteWebsite(Name)
                .ConfigureAwait(true);

            var isApplicationPoolDeleted = await iisInstallerService
                .DeleteApplicationPool(Name)
                .ConfigureAwait(true);

            isDone = isWebsiteDeleted && isApplicationPoolDeleted;
        }
        else
        {
            AddLogItem(LogLevel.Error, "Website is not removed");
        }

        var installedMainFile = new FileInfo(InstalledMainFilePath.GetValueAsString());
        Directory.Delete(installedMainFile.DirectoryName!, recursive: true);

        WorkOnAnalyzeAndUpdateStatesForVersion();

        return isDone;
    }

    private async Task ServiceDeployWebsiteStart()
    {
        await Task
            .Delay(TimeSpan.FromSeconds(1))
            .ConfigureAwait(false);

        RunningState = iisInstallerService.GetWebsiteState(Name);

        if (RunningState == ComponentRunningState.Stopped)
        {
            var isWebsiteStarted = await iisInstallerService
                .StartWebsiteAndApplicationPool(Name, Name)
                .ConfigureAwait(true);

            if (!isWebsiteStarted)
            {
                AddLogItem(LogLevel.Warning, "Website have some problem with startup");
            }

            RunningState = iisInstallerService.GetWebsiteState(Name);
        }
    }

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => EnableEditingMode = obj.EnableEditingMode;

    private async Task HandleCertificateIfNeeded()
    {
        if (!string.IsNullOrEmpty(HostName) &&
            HttpsPort is not null)
        {
            var certificate = iisInstallerService.GetX509Certificates()
                .FirstOrDefault(x => x.GetNameIdentifier() == ProjectName);
            if (certificate is null)
            {
                await CreateSelfSignedCertificateAndAssign(
                        subjectName: ProjectName,
                        friendlyName: ProjectName,
                        dnsName: HostName,
                        password: ProjectName,
                        yearsUntilExpiry: 100)
                    .ConfigureAwait(true);
            }
            else
            {
                iisInstallerService.AssignX509CertificateToWebsite(Name, certificate);
                X509Certificate = certificate;
            }
        }
    }

    private bool CanEditX509CertificateCommandHandler()
        => InstallationState is
            ComponentInstallationState.Installed or
            ComponentInstallationState.InstalledWithOldVersion;

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private void EditX509CertificateCommandHandler()
    {
        var iisCertificates = iisInstallerService.GetX509Certificates();

        var dialogBox = InputFormDialogBoxFactory.CreateForEditX509Certificate(
            iisCertificates,
            X509Certificate);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var data = dialogBox.Data.GetKeyValues();

        var thumbprint = data["Certificate"].ToString();

        if (X509Certificate?.Thumbprint != thumbprint)
        {
            if ("#Remove#".Equals(thumbprint, StringComparison.Ordinal))
            {
                iisInstallerService.UnAssignX509CertificateOnWebsite(Name);
                X509Certificate = null;
            }
            else
            {
                var certificate = iisCertificates.First(x => x.Thumbprint == thumbprint);
                iisInstallerService.AssignX509CertificateToWebsite(Name, certificate);
                X509Certificate = certificate;
            }
        }
    }

    private bool CanNewX509CertificateCommandHandler()
        => InstallationState is
            ComponentInstallationState.Installed or
            ComponentInstallationState.InstalledWithOldVersion;

    private async Task NewX509CertificateCommandHandler()
    {
        var dialogBox = InputFormDialogBoxFactory.CreateForNewX509Certificate();

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var data = dialogBox.Data.GetKeyValues();

        var friendlyName = data["Friendly"].ToString()!;
        var subjectName = data["Subject"].ToString()!;
        var dnsName = data["Dns"].ToString()!;
        var password = data["Password"].ToString()!;
        var yearsUntilExpiry = NumberHelper.ParseToInt(data["Years"].ToString()!);

        var certificates = iisInstallerService.GetX509Certificates();

        if (certificates.FirstOrDefault(x => x.GetNameIdentifier().Equals(friendlyName, StringComparison.OrdinalIgnoreCase)) is null &&
            certificates.FirstOrDefault(x => x.GetNameIdentifier().Equals(subjectName, StringComparison.OrdinalIgnoreCase)) is null)
        {
            await CreateSelfSignedCertificateAndAssign(
                    subjectName,
                    friendlyName,
                    dnsName,
                    password,
                    yearsUntilExpiry)
                .ConfigureAwait(true);
        }
    }

    private async Task CreateSelfSignedCertificateAndAssign(
        string subjectName,
        string friendlyName,
        string dnsName,
        string password,
        int yearsUntilExpiry)
    {
        X509Certificate = await CertificateStoreHelper
            .CreateSelfSignedCertificateAndAddToStore(
                subjectName,
                friendlyName,
                dnsName,
                password,
                yearsUntilExpiry)
            .ConfigureAwait(false);

        var openInternetBrowsers = InternetBrowserHelper.GetRunningInternetBrowsers();
        if (openInternetBrowsers.Any())
        {
            var message = $"We have to close internet browsers,{Environment.NewLine}" +
                          $"because internet browsers have to 'reload'{Environment.NewLine}" +
                          $"the new certificate.";

            var dialogBoxKill = new InfoDialogBox(
                Application.Current.MainWindow!,
                new DialogBoxSettings(DialogBoxType.Ok)
                {
                    TitleBarText = "Information",
                    Width = 380,
                    Height = 220,
                },
                message);
            dialogBoxKill.ShowDialog();

            InternetBrowserHelper.CloseMainWindowOnAllRunningInternetBrowsers();
        }
    }
}