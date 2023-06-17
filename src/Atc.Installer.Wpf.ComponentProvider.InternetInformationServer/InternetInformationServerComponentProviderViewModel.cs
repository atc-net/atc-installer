// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IInternetInformationServerInstallerService iisInstallerService;
    private readonly INetworkShellService networkShellService;

    public InternetInformationServerComponentProviderViewModel(
        IInternetInformationServerInstallerService internetInformationServerInstallerService,
        INetworkShellService networkShellService,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.iisInstallerService = internetInformationServerInstallerService ?? throw new ArgumentNullException(nameof(internetInformationServerInstallerService));
        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));

        if (InstallationPath is not null)
        {
            InstallationPath = iisInstallerService.ResolvedVirtuelRootFolder(InstallationPath)!;
        }

        if (InstalledMainFile is not null)
        {
            InstalledMainFile = iisInstallerService.ResolvedVirtuelRootFolder(InstalledMainFile);
        }

        IsRequiredWebSockets = applicationOption.DependentComponents.Contains("WebSockets", StringComparer.Ordinal);

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            HostName = hostNameValue;
        }

        if (TryGetUshortFromApplicationSettings("HttpPort", out var httpValue))
        {
            HttpPort = httpValue;
        }

        if (TryGetUshortFromApplicationSettings("HttpsPort", out var httpsValue))
        {
            HttpsPort = httpsValue;
        }
    }

    public bool IsRequiredWebSockets { get; }

    public string? HostName { get; }

    public ushort? HttpPort { get; }

    public ushort? HttpsPort { get; }

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

    [SuppressMessage("SonarRules", "S3440:Variables should not be checked against the values they're about to be assigned", Justification = "OK.")]
    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!iisInstallerService.IsInstalled)
        {
            return;
        }

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.PartialRunning &&
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
                ? ComponentRunningState.PartialRunning
                : ComponentRunningState.Unknown,
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

        foreach (var configurationSettingsFile in ConfigurationSettingsFiles)
        {
            if (!configurationSettingsFile.FileName.Equals(fileName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var setting in configurationSettingsFile.JsonSettings)
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

        foreach (var configurationSettingsFile in ConfigurationSettingsFiles)
        {
            if (!configurationSettingsFile.FileName.Equals(fileName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var setting in configurationSettingsFile.XmlSettings)
            {
                if (setting.Element.Equals("add", StringComparison.Ordinal) &&
                    setting.Path.EndsWith("appSettings", StringComparison.Ordinal))
                {
                    var attributeKey = setting.Attributes.FirstOrDefault(x => x.Key == "key");
                    var attributeValue = setting.Attributes.FirstOrDefault(x => x.Key == "value");
                    if (!string.IsNullOrEmpty(attributeKey.Value) &&
                        !string.IsNullOrEmpty(attributeValue.Value))
                    {
                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(attributeValue.Value);
                        xmlDocument.SetAppSettings(attributeKey.Value, value);
                    }
                }
                else
                {
                    foreach (var settingAttribute in setting.Attributes)
                    {
                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(settingAttribute.Value);
                        xmlDocument.SetValue(setting.Path, setting.Element, settingAttribute.Key, value);
                    }
                }
            }
        }
    }

    public override string ResolvedVirtuelRootFolder(
        string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);

        if (InstallationPath is not null)
        {
            folder = folder.Replace(".", InstallationPath, StringComparison.Ordinal);
        }

        return folder;
    }

    public override bool CanServiceStopCommandHandler()
        => RunningState == ComponentRunningState.Running;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Stop service"));

        var isStopped = await iisInstallerService
            .StopWebsiteApplicationPool(Name, Name)
            .ConfigureAwait(true);

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
        if (UnpackedZipPath is null)
        {
            return false;
        }

        return RunningState switch
        {
            ComponentRunningState.Stopped => true,
            ComponentRunningState.Unknown when InstallationState is ComponentInstallationState.NotInstalled or ComponentInstallationState.InstalledWithOldVersion => true,
            _ => false,
        };
    }

    public override Task ServiceDeployCommandHandler()
        => ServiceDeployAndStart(useAutoStart: false);

    public override Task ServiceDeployAndStartCommandHandler()
        => ServiceDeployAndStart(useAutoStart: true);

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
            UnpackedZipPath is not null &&
            InstallationPath is not null &&
            HttpPort.HasValue)
        {
            isDone = await ServiceDeployWebsiteCreate(useAutoStart).ConfigureAwait(true);
        }
        else if (RunningState == ComponentRunningState.Stopped &&
                 UnpackedZipPath is not null &&
                 InstallationPath is not null)
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

        if (UnpackedZipPath is null ||
            InstallationPath is null ||
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
                new DirectoryInfo(InstallationPath),
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

        if (UnpackedZipPath is null ||
            InstallationPath is null)
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

        if (HttpPort.HasValue)
        {
            await networkShellService
                .OpenHttpPortForEveryone(HttpPort.Value)
                .ConfigureAwait(false);
        }

        if (HttpsPort.HasValue)
        {
            await networkShellService
                .OpenHttpsPortForEveryone(HttpsPort.Value)
                .ConfigureAwait(false);
        }

        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting website"));
            await ServiceDeployWebsiteStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Website is started")
                : LogItemFactory.CreateWarning("Website is not started"));
        }
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