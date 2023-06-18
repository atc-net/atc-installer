// ReSharper disable InvertIf
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

public class WindowsApplicationComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private readonly INetworkShellService networkShellService;

    public WindowsApplicationComponentProviderViewModel(
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        INetworkShellService networkShellService,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            installerTempDirectory,
            installationDirectory,
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));

        if (applicationOption.ComponentType == ComponentType.WindowsService)
        {
            IsWindowsService = true;
            ServiceName = Name;
            if (applicationOption.DependentComponents.Count > 0)
            {
                ServiceName = applicationOption.DependentComponents[0];
            }

            DependentComponents = applicationOption.DependentComponents;
        }

        SettingsVisibility = HostingFramework == HostingFrameworkType.NativeNoSettings
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public bool IsWindowsService { get; }

    public Visibility SettingsVisibility { get; }

    public IList<string> DependentComponents { get; init; } = new List<string>();

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        RunningState = IsWindowsService
            ? waInstallerService.GetServiceState(ServiceName!)
            : waInstallerService.GetApplicationState(Name);

        if (RunningState == ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
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

        LogItems.Add(LogItemFactory.CreateTrace("Stop"));

        if (IsWindowsService)
        {
            var isStopped = await waInstallerService
                .StopService(ServiceName!)
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
        }
        else
        {
            var isStopped = waInstallerService
                .StopApplication(InstalledMainFilePath!);

            if (isStopped)
            {
                RunningState = ComponentRunningState.Stopped;
                LogAndSendToastNotificationMessage(
                    ToastNotificationType.Information,
                    Name,
                    "Application is stopped");
            }
            else
            {
                LogAndSendToastNotificationMessage(
                    ToastNotificationType.Error,
                    Name,
                    "Could not stop application");
            }
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

        if (IsWindowsService)
        {
            var isStarted = await waInstallerService
                .StartService(ServiceName!)
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
        }
        else
        {
            var isStarted = waInstallerService
                .StartApplication(InstalledMainFilePath!);

            if (isStarted)
            {
                RunningState = ComponentRunningState.Running;
                LogAndSendToastNotificationMessage(
                    ToastNotificationType.Information,
                    Name,
                    "Application is started");
            }
            else
            {
                LogAndSendToastNotificationMessage(
                    ToastNotificationType.Error,
                    Name,
                    "Could not start application");
            }
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
            _ => false,
        };
    }

    public override Task ServiceDeployCommandHandler()
        => ServiceDeployAndStart(useAutoStart: false);

    public override Task ServiceDeployAndStartCommandHandler()
        => ServiceDeployAndStart(useAutoStart: true);

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        CheckPrerequisitesForHostingFramework();
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

        if (InstallationFolderPath is not null)
        {
            folder = folder.Replace(".", InstallationFolderPath, StringComparison.Ordinal);
        }

        return folder;
    }

    private void CheckPrerequisitesForHostingFramework()
    {
        switch (HostingFramework)
        {
            case HostingFrameworkType.DotNet7 when waInstallerService.IsMicrosoftDonNet7():
                AddToInstallationPrerequisites("IsMicrosoftDonNet7", LogCategoryType.Information, "Microsoft .NET 7 is installed");
                break;
            case HostingFrameworkType.DotNet7:
                AddToInstallationPrerequisites("IsMicrosoftDonNet7", LogCategoryType.Warning, "Microsoft .NET 7 is not installed");
                break;
            case HostingFrameworkType.DonNetFramework48 when waInstallerService.IsMicrosoftDonNetFramework48():
                AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Information, "Microsoft .NET Framework 4.8 is installed");
                break;
            case HostingFrameworkType.DonNetFramework48:
                AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Warning, "Microsoft .NET Framework 4.8 is not installed");
                break;
            case HostingFrameworkType.Native:
            case HostingFrameworkType.NativeNoSettings:
                if (IsWindowsService)
                {
                    var runningState = waInstallerService.GetServiceState(ServiceName!);
                    if (runningState == ComponentRunningState.Unknown)
                    {
                        AddToInstallationPrerequisites($"Is{ServiceName}", LogCategoryType.Warning, $"{ServiceName} is not installed");
                    }
                    else
                    {
                        AddToInstallationPrerequisites($"Is{ServiceName}", LogCategoryType.Information, $"{ServiceName} is installed");
                        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;
                        RunningState = runningState;
                    }

                    foreach (var dependentComponent in DependentComponents)
                    {
                        if (dependentComponent.Equals(ServiceName!, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        runningState = waInstallerService.GetServiceState(dependentComponent);
                        if (runningState != ComponentRunningState.Unknown)
                        {
                            AddToInstallationPrerequisites($"Is{dependentComponent}", LogCategoryType.Information, $"{dependentComponent} is installed");
                        }
                        else
                        {
                            AddToInstallationPrerequisites($"Is{dependentComponent}", LogCategoryType.Warning, $"{dependentComponent} is not installed");
                        }
                    }
                }

                break;
        }
    }

    private void AddToInstallationPrerequisites(
        string key,
        LogCategoryType categoryType,
        string message)
    {
        InstallationPrerequisites.Add(
            new InstallationPrerequisiteViewModel(
                $"WA_{key}",
                categoryType,
                message));
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

        if (IsWindowsService)
        {
            if (InstallationState == ComponentInstallationState.NotInstalled &&
                UnpackedZipFolderPath is not null &&
                InstallationFolderPath is not null)
            {
                isDone = await ServiceDeployWindowServiceCreate(useAutoStart).ConfigureAwait(true);
            }
            else if (RunningState == ComponentRunningState.Stopped &&
                     UnpackedZipFolderPath is not null &&
                     InstallationFolderPath is not null)
            {
                isDone = await ServiceDeployWindowServiceUpdate(useAutoStart).ConfigureAwait(true);
            }
        }
        else
        {
            if (InstallationState == ComponentInstallationState.NotInstalled &&
                UnpackedZipFolderPath is not null &&
                InstallationFolderPath is not null)
            {
                isDone = await ServiceDeployWindowApplicationCreate().ConfigureAwait(true);
            }
            else if (UnpackedZipFolderPath is not null &&
                     InstallationFolderPath is not null)
            {
                isDone = ServiceDeployWindowApplicationUpdate();
            }
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

    private async Task<bool> ServiceDeployWindowServiceCreate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return isDone;
        }

        await ServiceDeployWindowServicePostProcessing(useAutoStart).ConfigureAwait(true);

        isDone = true;

        return isDone;
    }

    private async Task<bool> ServiceDeployWindowServiceUpdate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        await ServiceDeployWindowServicePostProcessing(useAutoStart).ConfigureAwait(true);

        isDone = true;

        return isDone;
    }

    private async Task ServiceDeployWindowServicePostProcessing(
        bool useAutoStart)
    {
        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        if (TryGetStringFromApplicationSettings("WebProtocol", out _) &&
            TryGetUshortFromApplicationSettings("HttpPort", out var httpPortValue))
        {
            await networkShellService
                .OpenHttpPortForEveryone(httpPortValue)
                .ConfigureAwait(false);
        }

        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting service"));
            await ServiceDeployWindowServiceStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Service is started")
                : LogItemFactory.CreateWarning("Service is not started"));
        }
    }

    private async Task ServiceDeployWindowServiceStart()
    {
        await Task
            .Delay(100)
        .ConfigureAwait(false);

        RunningState = waInstallerService.GetServiceState(Name);

        if (RunningState == ComponentRunningState.Stopped)
        {
            var isWebsiteStarted = await waInstallerService
                .StartService(Name)
                .ConfigureAwait(true);

            if (!isWebsiteStarted)
            {
                LogItems.Add(LogItemFactory.CreateWarning("Website have some problem with startup"));
            }

            RunningState = waInstallerService.GetServiceState(Name);
        }
    }

    private Task<bool> ServiceDeployWindowApplicationCreate()
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return Task.FromResult(isDone);
        }

        CopyUnpackedFiles();

        isDone = true;

        return Task.FromResult(isDone);
    }

    private bool ServiceDeployWindowApplicationUpdate()
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        CopyUnpackedFiles();

        isDone = true;

        return isDone;
    }
}