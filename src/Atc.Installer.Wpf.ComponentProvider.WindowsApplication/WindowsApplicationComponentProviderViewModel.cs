// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
public class WindowsApplicationComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IWindowsApplicationInstallerService waInstallerService;

    public WindowsApplicationComponentProviderViewModel(
        ILoggerFactory loggerFactory,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        INetworkShellService networkShellService,
        IWindowsFirewallService windowsFirewallService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        ObservableCollectionEx<KeyValueTemplateItemViewModel> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            loggerFactory,
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

        this.waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));

        InitializeFromApplicationOptions(applicationOption);
    }

    public bool IsWindowsService { get; private set; }

    public Visibility SettingsVisibility { get; private set; }

    public IList<string> DependentComponents { get; private set; } = new List<string>();

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        RunningStateIssues.SuppressOnChangedNotification = true;
        RunningStateIssues.Clear();

        if (IsWindowsService)
        {
            RunningState = waInstallerService.GetServiceState(ServiceName!);
        }
        else
        {
            RunningState = waInstallerService.GetApplicationState(Name);
            if (RunningState == ComponentRunningState.NotAvailable &&
                InstallationState is ComponentInstallationState.Installed or ComponentInstallationState.InstalledWithOldVersion)
            {
                RunningState = ComponentRunningState.Stopped;
            }
        }

        if (RunningState == ComponentRunningState.Running &&
            DependentServices.Any(x => x.RunningState != ComponentRunningState.Running))
        {
            RunningState = ComponentRunningState.PartiallyRunning;
            ApplyDependentServicesToRunningStateIssues();
        }

        if (RunningState is ComponentRunningState.Unknown or ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }

        RunningStateIssues.SuppressOnChangedNotification = false;
    }

    public override bool CanServiceStopCommandHandler()
        => !DisableInstallationActions &&
           !HideMenuItem &&
           RunningState == ComponentRunningState.Running;

    public override async Task ServiceStopCommandHandler()
    {
        Messenger.Default.Send(new UpdateUserActionTimestampMessage());

        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        if (IsWindowsService)
        {
            AddLogItem(LogLevel.Trace, "Stop service");

            var isStopped = await waInstallerService
                .StopService(ServiceName!)
                .ConfigureAwait(true);

            if (isStopped ||
                waInstallerService.GetServiceState(ServiceName!) == ComponentRunningState.Stopped)
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
            AddLogItem(LogLevel.Trace, "Stop application");

            var isStopped = waInstallerService
                .StopApplication(InstalledMainFilePath!.GetValueAsString());

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
        => !DisableInstallationActions &&
           !HideMenuItem &&
           RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        Messenger.Default.Send(new UpdateUserActionTimestampMessage());

        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Start service");

        if (IsWindowsService)
        {
            await StartWindowsService().ConfigureAwait(false);
        }
        else
        {
            StartApplication();
        }

        IsBusy = false;
    }

    public override bool CanServiceDeployCommandHandler()
    {
        if (DisableInstallationActions ||
            HideMenuItem ||
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
            HideMenuItem ||
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
        Messenger.Default.Send(new UpdateUserActionTimestampMessage());

        if (!CanServiceRemoveCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Remove");

        var isDone = false;

        if (IsWindowsService)
        {
            if (InstallationState is
                    ComponentInstallationState.Installed or
                    ComponentInstallationState.InstalledWithOldVersion &&
                InstallationFolderPath is not null)
            {
                isDone = await ServiceRemoveWindowService().ConfigureAwait(true);
            }
        }
        else
        {
            if (InstallationState is
                    ComponentInstallationState.Installed or
                    ComponentInstallationState.InstalledWithOldVersion &&
                InstallationFolderPath is not null)
            {
                isDone = await ServiceRemoveWindowApplication().ConfigureAwait(true);
            }
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

    public override string Description
    {
        get
        {
            if (IsDotNetWpf())
            {
                return $"{base.Description} / WPF";
            }

            return base.Description;
        }
    }

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
                        var value = ResolveTemplateIfNeededByApplicationSettingsLookup(attributeValue.Value.ToString()!);
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
            folder = folder.Replace(".", InstallationFolderPath.GetValueAsString(), StringComparison.Ordinal);
        }

        return folder;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private void InitializeFromApplicationOptions(
        ApplicationOption applicationOption)
    {
        if (applicationOption.ComponentType == ComponentType.WindowsService)
        {
            IsWindowsService = true;
            ServiceName = string.IsNullOrEmpty(applicationOption.ServiceName)
                ? Name
                : applicationOption.ServiceName;
            DependentComponents = applicationOption.DependentComponents;
        }

        SettingsVisibility = HostingFramework == HostingFrameworkType.NativeNoSettings
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (TryGetStringFromApplicationSettings("WebProtocol", out _) &&
            TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            if (TryGetUshortFromApplicationSettings("HttpPort", out var httpPortValue))
            {
                if (TryGetBooleanFromApplicationSettings("SwaggerEnabled", out var swaggerEnabledValue) &&
                    swaggerEnabledValue)
                {
                    Endpoints.Add(
                        new EndpointViewModel(
                            "Http - Swagger",
                            ComponentEndpointType.BrowserLink,
                            $"http://{hostNameValue}:{httpPortValue}/swagger",
                            "http://[[HostName]]:[[HttpPort]]/swagger",
                            new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                }
                else
                {
                    Endpoints.Add(
                        new EndpointViewModel(
                            "Http",
                            ComponentEndpointType.BrowserLink,
                            $"http://{hostNameValue}:{httpPortValue}",
                            "http://[[HostName]]:[[HttpPort]]",
                            new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                }
            }

            if (TryGetUshortFromApplicationSettings("HttpsPort", out var httpsPortValue))
            {
                if (TryGetBooleanFromApplicationSettings("SwaggerEnabled", out var swaggerEnabledValue) &&
                    swaggerEnabledValue)
                {
                    Endpoints.Add(
                        new EndpointViewModel(
                            "Https - Swagger",
                            ComponentEndpointType.BrowserLink,
                            $"https://{hostNameValue}:{httpsPortValue}/swagger",
                            "https://[[HostName]]:[[HttpsPort]]/swagger",
                            new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
                }
                else
                {
                    Endpoints.Add(
                        new EndpointViewModel(
                            "Https",
                            ComponentEndpointType.BrowserLink,
                            $"https://{hostNameValue}:{httpsPortValue}",
                            "https://[[HostName]]:[[HttpsPort]]",
                            new List<string> { "DefaultApplicationSettings", "ApplicationSettings" }));
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

            if (endpoint.Endpoint.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
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

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private void CheckPrerequisitesForHostingFramework()
    {
        switch (HostingFramework)
        {
            case HostingFrameworkType.DotNet7 when waInstallerService.IsMicrosoftDotNet7():
                AddToInstallationPrerequisites("IsMicrosoftDotNet7", LogCategoryType.Information, "Microsoft .NET 7 is installed");
                if (IsDotNetWpf())
                {
                    if (waInstallerService.IsComponentInstalledMicrosoftNetDesktop7())
                    {
                        AddToInstallationPrerequisites("IsMicrosoftDotNet7Desktop", LogCategoryType.Information, "Microsoft .NET 7 Windows Desktop is installed");
                    }
                    else
                    {
                        AddToInstallationPrerequisites("IsMicrosoftDotNet7Desktop", LogCategoryType.Warning, "Microsoft .NET 7 Windows Desktop is not installed");
                    }
                }

                break;
            case HostingFrameworkType.DotNet7:
                AddToInstallationPrerequisites("IsMicrosoftDotNet7", LogCategoryType.Warning, "Microsoft .NET 7 is not installed");
                break;
            case HostingFrameworkType.DotNet8 when waInstallerService.IsMicrosoftDotNet8():
                AddToInstallationPrerequisites("IsMicrosoftDotNet8", LogCategoryType.Information, "Microsoft .NET 8 is installed");
                if (IsDotNetWpf())
                {
                    if (waInstallerService.IsComponentInstalledMicrosoftNetDesktop8())
                    {
                        AddToInstallationPrerequisites("IsMicrosoftDotNet8Desktop", LogCategoryType.Information, "Microsoft .NET 8 Windows Desktop is installed");
                    }
                    else
                    {
                        AddToInstallationPrerequisites("IsMicrosoftDotNet8Desktop", LogCategoryType.Warning, "Microsoft .NET 8 Windows Desktop is not installed");
                    }
                }

                break;
            case HostingFrameworkType.DotNet8:
                AddToInstallationPrerequisites("IsMicrosoftDotNet8", LogCategoryType.Warning, "Microsoft .NET 8 is not installed");
                break;
            case HostingFrameworkType.DotNetFramework48 when waInstallerService.IsMicrosoftDotNetFramework48():
                AddToInstallationPrerequisites("IsMicrosoftDotNetFramework48", LogCategoryType.Information, "Microsoft .NET Framework 4.8 is installed");
                break;
            case HostingFrameworkType.DotNetFramework48:
                AddToInstallationPrerequisites("IsMicrosoftDotNetFramework48", LogCategoryType.Warning, "Microsoft .NET Framework 4.8 is not installed");
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
                        InstallationState = ComponentInstallationState.Installed;
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

    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
    private bool IsDotNetWpf()
    {
        var mainFilePath = string.Empty;
        if (UnpackedZipFolderPath is not null)
        {
            var filePath = Path.Combine(UnpackedZipFolderPath, $"{Name}.exe");
            if (File.Exists(filePath))
            {
                mainFilePath = filePath;
            }
        }
        else if (InstalledMainFilePath is not null)
        {
            var filePath = InstalledMainFilePath.GetValueAsString();
            if (File.Exists(filePath))
            {
                mainFilePath = filePath;
            }
        }

        if (string.IsNullOrEmpty(mainFilePath))
        {
            return false;
        }

        var mainDllFilePath = mainFilePath.Replace(".exe", ".dll", StringComparison.OrdinalIgnoreCase);
        if (!File.Exists(mainDllFilePath))
        {
            return false;
        }

        try
        {
            var assembly = AssemblyHelper.Load(new FileInfo(mainDllFilePath));
            if (assembly
                .GetReferencedAssemblies()
                .Any(x => x.Name!.Equals("PresentationFramework", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        catch
        {
            // Dummy
        }

        return false;
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
        Messenger.Default.Send(new UpdateUserActionTimestampMessage());

        if (!CanServiceDeployCommandHandler())
        {
            return;
        }

        await SetIsBusy(value: true, delayInMs: 500).ConfigureAwait(true);

        AddLogItem(LogLevel.Trace, "Deploy");

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
                isDone = await ServiceDeployWindowApplicationCreate(useAutoStart).ConfigureAwait(true);
            }
            else if (UnpackedZipFolderPath is not null &&
                     InstallationFolderPath is not null)
            {
                isDone = ServiceDeployWindowApplicationUpdate(useAutoStart);
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

        WorkOnAnalyzeAndUpdateStatesForVersion();

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

        WorkOnAnalyzeAndUpdateStatesForVersion();

        isDone = true;

        return isDone;
    }

    private static async Task<bool> SetupInstalledMainFilePathAsService(
        FileInfo installedMainFile)
    {
        var (isSuccessful, output) = await ProcessHelper
            .Execute(
                installedMainFile.Directory!,
                new FileInfo(installedMainFile.FullName),
                "install",
                runAsAdministrator: true)
            .ConfigureAwait(true);

        return isSuccessful &&
               output is not null &&
               output.Contains("successfully installed", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ServiceRemoveWindowService()
    {
        if (InstalledMainFilePath is null)
        {
            return false;
        }

        var installedMainFile = new FileInfo(InstalledMainFilePath.GetValueAsString());
        var (isSuccessful, output) = await ProcessHelper
            .Execute(
                installedMainFile.Directory!,
                new FileInfo(installedMainFile.FullName),
                "uninstall")
            .ConfigureAwait(true);

        if (!isSuccessful ||
            output is null ||
            !output.Contains("successfully removed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Directory.Delete(installedMainFile.DirectoryName!, recursive: true);

        WorkOnAnalyzeAndUpdateStatesForVersion();

        return true;
    }

    private async Task ServiceDeployWindowServicePostProcessing(
        bool useAutoStart)
    {
        BackupConfigurationFilesAndLog();

        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        EnsureRegistrySettings();

        EnsureFirewallRules();

        if (TryGetStringFromApplicationSettings("WebProtocol", out _))
        {
            if (TryGetUshortFromApplicationSettings("HttpPort", out var httpPortValue))
            {
                await EnsureUrlReservationEntryIfNeeded("http", httpPortValue).ConfigureAwait(true);
            }

            if (TryGetUshortFromApplicationSettings("HttpsPort", out var httpsPortValue))
            {
                await EnsureUrlReservationEntryIfNeeded("https", httpsPortValue).ConfigureAwait(true);
            }
        }

        InstallationState = ComponentInstallationState.Installed;

        if (RunningState == ComponentRunningState.NotAvailable &&
            InstalledMainFilePath is not null &&
            File.Exists(InstalledMainFilePath.GetValueAsString()) &&
            InstalledMainFilePath.GetValueAsString().EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var isInstalled = await SetupInstalledMainFilePathAsService(new FileInfo(InstalledMainFilePath.GetValueAsString())).ConfigureAwait(true);

            if (isInstalled)
            {
                AddLogItem(LogLevel.Information, "Service is installed");
                RunningState = waInstallerService.GetServiceState(ServiceName!);
            }
            else
            {
                AddLogItem(LogLevel.Error, "Service is not installed");
            }
        }

        if (useAutoStart)
        {
            AddLogItem(LogLevel.Trace, "Auto starting service");
            await ServiceDeployWindowServiceStart().ConfigureAwait(true);
            if (RunningState == ComponentRunningState.Running)
            {
                AddLogItem(LogLevel.Information, "Service is started");
            }
            else
            {
                AddLogItem(LogLevel.Warning, "Service is not started");
            }
        }
    }

    private async Task ServiceDeployWindowServiceStart()
    {
        await Task.Delay(100).ConfigureAwait(false);

        RunningState = waInstallerService.GetServiceState(Name);

        if (RunningState == ComponentRunningState.Stopped)
        {
            var isWebsiteStarted = await waInstallerService
                .StartService(Name)
                .ConfigureAwait(true);

            if (!isWebsiteStarted)
            {
                AddLogItem(LogLevel.Warning, "Website have some problem with startup");
            }

            RunningState = waInstallerService.GetServiceState(Name);
        }
    }

    private Task<bool> ServiceDeployWindowApplicationCreate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return Task.FromResult(isDone);
        }

        BackupConfigurationFilesAndLog();

        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        EnsureRegistrySettings();

        EnsureFirewallRules();

        InstallationState = ComponentInstallationState.Installed;

        if (useAutoStart)
        {
            StartApplication();
        }

        WorkOnAnalyzeAndUpdateStatesForVersion();

        isDone = true;

        return Task.FromResult(isDone);
    }

    private bool ServiceDeployWindowApplicationUpdate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        CopyUnpackedFiles();

        UpdateConfigurationFiles();

        EnsureFolderPermissions();

        EnsureRegistrySettings();

        EnsureFirewallRules();

        InstallationState = ComponentInstallationState.Installed;

        if (useAutoStart)
        {
            StartApplication();
        }

        WorkOnAnalyzeAndUpdateStatesForVersion();

        isDone = true;

        return isDone;
    }

    private Task<bool> ServiceRemoveWindowApplication()
    {
        if (InstalledMainFilePath is null)
        {
            return Task.FromResult(false);
        }

        var installedMainFile = new FileInfo(InstalledMainFilePath.GetValueAsString());

        Directory.Delete(installedMainFile.DirectoryName!, recursive: true);

        InstallationState = ComponentInstallationState.NotInstalled;

        WorkOnAnalyzeAndUpdateStatesForVersion();

        return Task.FromResult(true);
    }

    private async Task StartWindowsService()
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

    private void StartApplication()
    {
        var isStarted = waInstallerService
            .StartApplication(new FileInfo(InstalledMainFilePath!.GetValueAsString()));

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
}