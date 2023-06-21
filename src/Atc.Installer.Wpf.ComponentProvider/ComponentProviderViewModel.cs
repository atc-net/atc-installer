// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable InvertIf
// ReSharper disable StringLiteralTypo
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private readonly ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders;
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;
    private string? installationFile;
    private string? unpackedZipFolderPath;
    private string? installationFolderPath;
    private string? installedMainFilePath;
    private string? installedVersion;
    private string? installationVersion;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            refComponentProviders = new ObservableCollectionEx<ComponentProviderViewModel>();
            InstallationState = ComponentInstallationState.Checking;
            InstallerTempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "atc-installer"));
            InstallationDirectory = new DirectoryInfo(Path.Combine(InstallerTempDirectory.FullName, "InstallationFiles"));
            ProjectName = "MyProject";
            Name = "MyApp";
            InstallationFolderPath = @"C:\ProgramFiles\MyApp";
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviders);
        ArgumentException.ThrowIfNullOrEmpty(projectName);
        ArgumentNullException.ThrowIfNull(defaultApplicationSettings);
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.refComponentProviders = refComponentProviders;
        InstallerTempDirectory = installerTempDirectory;
        InstallationDirectory = installationDirectory;
        ProjectName = projectName;
        DefaultApplicationSettings.Populate(defaultApplicationSettings);
        ApplicationSettings.Populate(applicationOption.ApplicationSettings);
        FolderPermissions.Populate(applicationOption.FolderPermissions);
        ConfigurationSettingsFiles = applicationOption.ConfigurationSettingsFiles;
        Name = applicationOption.Name;
        ComponentType = applicationOption.ComponentType;
        HostingFramework = applicationOption.HostingFramework;
        IsService = applicationOption.ComponentType is ComponentType.PostgreSqlServer or ComponentType.InternetInformationService or ComponentType.WindowsService;
        InstallationFile = applicationOption.InstallationFile;
        InstallationFolderPath = applicationOption.InstallationPath;
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public DirectoryInfo InstallerTempDirectory { get; }

    public DirectoryInfo InstallationDirectory { get; }

    public string ProjectName { get; }

    public ApplicationSettingsViewModel DefaultApplicationSettings { get; } = new();

    public ApplicationSettingsViewModel ApplicationSettings { get; } = new();

    public FolderPermissionsViewModel FolderPermissions { get; } = new();

    public IList<ConfigurationSettingsFileOption> ConfigurationSettingsFiles { get; } = new List<ConfigurationSettingsFileOption>();

    public string Name { get; }

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; }

    public HostingFrameworkType HostingFramework { get; }

    public bool IsService { get; }

    public string? InstallationFile
    {
        get => installationFile;
        protected set
        {
            installationFile = value;
            RaisePropertyChanged();
        }
    }

    public string? UnpackedZipFolderPath
    {
        get => unpackedZipFolderPath;
        protected set
        {
            unpackedZipFolderPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstallationFolderPath
    {
        get => installationFolderPath;
        protected set
        {
            installationFolderPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstalledMainFilePath
    {
        get => installedMainFilePath;
        protected set
        {
            installedMainFilePath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstalledVersion
    {
        get => installedVersion;
        protected set
        {
            installedVersion = value;
            RaisePropertyChanged();
        }
    }

    public string? InstallationVersion
    {
        get => installationVersion;
        protected set
        {
            installationVersion = value;
            RaisePropertyChanged();
        }
    }

    public IDictionary<FileInfo, DynamicJson> ConfigurationJsonFiles { get; } = new Dictionary<FileInfo, DynamicJson>();

    public IDictionary<FileInfo, XmlDocument> ConfigurationXmlFiles { get; } = new Dictionary<FileInfo, XmlDocument>();

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
            if (installationState == value)
            {
                return;
            }

            installationState = value;
            RaisePropertyChanged();

            Messenger.Default.Send(
                new UpdateDependentServiceStateMessage(
                Name,
                InstallationState,
                RunningState));
        }
    }

    public ComponentRunningState RunningState
    {
        get => runningState;
        set
        {
            if (runningState == value)
            {
                return;
            }

            runningState = value;
            RaisePropertyChanged();

            Messenger.Default.Send(
                new UpdateDependentServiceStateMessage(
                    Name,
                    InstallationState,
                    RunningState));
        }
    }

    public ObservableCollectionEx<LogItem> LogItems { get; } = new();

    public ObservableCollectionEx<InstallationPrerequisiteViewModel> InstallationPrerequisites { get; } = new();

    public int InstallationPrerequisiteIssueCount
        => InstallationPrerequisites.Count(x => x.CategoryType == LogCategoryType.Error);

    public ObservableCollectionEx<DependentServiceViewModel> DependentServices { get; } = new();

    public ObservableCollectionEx<EndpointViewModel> Endpoints { get; } = new();

    public ObservableCollection<EndpointViewModel> BrowserLinkEndpoints
        => new(Endpoints.Where(x => x.EndpointType == ComponentEndpointType.BrowserLink));

    public int DependentServicesIssueCount
        => DependentServices.Count(x => x.RunningState != ComponentRunningState.Running);

    public bool TryGetStringFromApplicationSettings(
        string key,
        out string value)
    {
        if (ApplicationSettings.TryGetString(key, out value) &&
            !string.IsNullOrWhiteSpace(value) &&
            !value.Equals($"[[{key}]]", StringComparison.Ordinal))
        {
            return true;
        }

        return DefaultApplicationSettings.TryGetString(key, out value) &&
               !string.IsNullOrWhiteSpace(value);
    }

    public bool TryGetBooleanFromApplicationSettings(
        string key,
        out bool value)
    {
        if (ApplicationSettings.TryGetBoolean(key, out value) &&
            value != default)
        {
            return true;
        }

        return DefaultApplicationSettings.TryGetBoolean(key, out value) &&
               value != default;
    }

    public bool TryGetUshortFromApplicationSettings(
        string key,
        out ushort value)
    {
        if (ApplicationSettings.TryGetUshort(key, out value) &&
            value != default)
        {
            return true;
        }

        return DefaultApplicationSettings.TryGetUshort(key, out value) &&
               value != default;
    }

    public void LogAndSendToastNotificationMessage(
        ToastNotificationType toastNotificationType,
        string title,
        string message)
    {
        switch (toastNotificationType)
        {
            case ToastNotificationType.Success:
            case ToastNotificationType.Information:
                LogItems.Add(LogItemFactory.CreateInformation(message));
                break;
            case ToastNotificationType.Warning:
                LogItems.Add(LogItemFactory.CreateWarning(message));
                break;
            case ToastNotificationType.Error:
                LogItems.Add(LogItemFactory.CreateError(message));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(toastNotificationType), toastNotificationType, message: null);
        }

        Messenger.Default.Send(
            new ToastNotificationMessage(
                ToastNotificationType.Information,
                title,
                message));
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    public void LoadConfigurationFiles()
    {
        if (InstallationFolderPath is null)
        {
            return;
        }

        switch (HostingFramework)
        {
            case HostingFrameworkType.DonNetFramework48:
            {
                if (InstalledMainFilePath is not null)
                {
                    var mainAppConfigFile = new FileInfo(InstalledMainFilePath + ".config");
                    if (mainAppConfigFile.Exists)
                    {
                        var xml = FileHelper.ReadAllText(mainAppConfigFile);
                        var xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(xml);
                        ConfigurationXmlFiles.Add(mainAppConfigFile, xmlDocument);
                    }
                }

                var appConfigFile = new FileInfo(Path.Combine(InstallationFolderPath, "app.config"));
                if (appConfigFile.Exists)
                {
                    var xml = FileHelper.ReadAllText(appConfigFile);
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    ConfigurationXmlFiles.Add(appConfigFile, xmlDocument);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationFolderPath, "web.config"));
                if (webConfigFile.Exists)
                {
                    var xml = FileHelper.ReadAllText(webConfigFile);
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    ConfigurationXmlFiles.Add(webConfigFile, xmlDocument);
                }

                break;
            }

            case HostingFrameworkType.DotNet7:
            {
                var appSettingsFile = new FileInfo(Path.Combine(InstallationFolderPath, "appsettings.json"));
                if (appSettingsFile.Exists)
                {
                    var dynamicJson = new DynamicJson(appSettingsFile);
                    ConfigurationJsonFiles.Add(appSettingsFile, dynamicJson);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationFolderPath, "web.config"));
                if (webConfigFile.Exists)
                {
                    var xml = FileHelper.ReadAllText(webConfigFile);
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    ConfigurationXmlFiles.Add(webConfigFile, xmlDocument);
                }

                break;
            }

            case HostingFrameworkType.NodeJs:
            {
                var envFile = new FileInfo(Path.Combine(InstallationFolderPath, "env.json"));
                if (envFile.Exists)
                {
                    var dynamicJson = new DynamicJson(envFile);
                    ConfigurationJsonFiles.Add(envFile, dynamicJson);
                }

                break;
            }

            default:
                throw new SwitchCaseDefaultException(HostingFramework);
        }
    }

    public void PrepareInstallationFiles(
        bool unpackIfIfExist)
    {
        if (InstallationFile is null ||
            !InstallationFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var installationFilePath = Path.Combine(InstallationDirectory.FullName, InstallationFile);
        if (!File.Exists(installationFilePath))
        {
            return;
        }

        UnpackedZipFolderPath = Path.Combine(InstallerTempDirectory.FullName, @$"{ProjectName}\Unpacked\{Name}");

        if (!unpackIfIfExist &&
            Directory.Exists(UnpackedZipFolderPath))
        {
            return;
        }

        if (Directory.Exists(UnpackedZipFolderPath))
        {
            Directory.Delete(UnpackedZipFolderPath, recursive: true);
        }

        Directory.CreateDirectory(UnpackedZipFolderPath);

        ZipFile.ExtractToDirectory(installationFilePath, UnpackedZipFolderPath, overwriteFiles: true);

        var filesToDelete = Directory.GetFiles(UnpackedZipFolderPath, "appsettings.*.json");
        foreach (var file in filesToDelete)
        {
            File.Delete(file);
        }
    }

    public void AnalyzeAndUpdateStatesInBackgroundThread()
    {
        Task.Run(async () =>
        {
            IsBusy = true;

            await Task
                .CompletedTask
                .ConfigureAwait(true);

            WorkOnAnalyzeAndUpdateStates();

            IsBusy = false;
        });
    }

    public virtual void CheckPrerequisites() { }

    public virtual void CheckServiceState() { }

    public virtual bool TryGetStringFromApplicationSetting(
        string key,
        out string resultValue)
    {
        resultValue = string.Empty;
        return false;
    }

    public virtual void UpdateConfigurationDynamicJson(
        string fileName,
        DynamicJson dynamicJson)
    { }

    public virtual string ResolvedVirtuelRootFolder(
        string folder)
    {
        return string.Empty;
    }

    public virtual void UpdateConfigurationXmlDocument(
        string fileName,
        XmlDocument xmlDocument)
    { }

    protected void BackupConfigurationFilesAndLog()
    {
        if (InstallationFolderPath is null)
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Backup files"));

        var timestamp = DateTime.Now.ToString("yyyyMMdd_hhmmss", GlobalizationConstants.EnglishCultureInfo);
        var backupFolder = Path.Combine(InstallerTempDirectory.FullName, @$"{ProjectName}\Backup\{Name}");
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var sourceAppSettingsFile = Path.Combine(InstallationFolderPath, "env.json");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"env_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationFolderPath, "appsettings.json");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"appsettings_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationFolderPath, "web.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"web_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationFolderPath, $"{Name}.exe.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"{Name}.exe_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }
    }

    protected void CopyUnpackedFiles()
    {
        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Copy files"));

        ResolveDirectoriesForFolderPermissions();

        var useDeleteBeforeCopy = !FolderPermissions
            .Items
            .Any(x => x.Directory is not null &&
                      x.Directory.FullName.StartsWith(InstallationFolderPath, StringComparison.OrdinalIgnoreCase));

        var directoryUnpackedZip = new DirectoryInfo(UnpackedZipFolderPath);
        var directoryInstallation = new DirectoryInfo(InstallationFolderPath);

        if (useDeleteBeforeCopy)
        {
            directoryUnpackedZip.CopyAll(directoryInstallation);
        }
        else
        {
            var excludeDirectories = FolderPermissions
                .Items
                .Where(x => x.Directory is not null &&
                            x.Directory.FullName.StartsWith(InstallationFolderPath, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Directory!.Name)
                .ToList();

            directoryInstallation.DeleteAllFiles();
            directoryInstallation.DeleteAllDirectories("*", useRecursive: true, excludeDirectories);
            directoryUnpackedZip.CopyAll(directoryInstallation, useRecursive: true, deleteAllFromDestinationBeforeCopy: false);
        }

        LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));
    }

    protected void UpdateConfigurationFiles()
    {
        LogItems.Add(LogItemFactory.CreateTrace("Update configuration files"));

        LoadConfigurationFiles();

        foreach (var configurationJsonFile in ConfigurationJsonFiles)
        {
            UpdateConfigurationDynamicJson(configurationJsonFile.Key.Name, configurationJsonFile.Value);

            var jsonSource = FileHelper.ReadAllText(configurationJsonFile.Key);
            var jsonTarget = configurationJsonFile.Value.ToJson(orderByKey: true);
            if (jsonSource != jsonTarget)
            {
                FileHelper.WriteAllText(configurationJsonFile.Key, jsonTarget);
            }
        }

        foreach (var configurationXmlFile in ConfigurationXmlFiles)
        {
            UpdateConfigurationXmlDocument(configurationXmlFile.Key.Name, configurationXmlFile.Value);

            var xmlSource = FileHelper.ReadAllText(configurationXmlFile.Key);
            var xmlTarget = configurationXmlFile.Value.ToIndentedXml();
            if (xmlSource != xmlTarget)
            {
                FileHelper.WriteAllText(configurationXmlFile.Key, xmlTarget);
            }
        }

        LogItems.Add(LogItemFactory.CreateInformation("Configuration files is updated copied"));
    }

    protected void EnsureFolderPermissions()
    {
        LogItems.Add(LogItemFactory.CreateTrace("Ensure folder permissions"));

        ResolveDirectoriesForFolderPermissions();

        foreach (var vm in FolderPermissions.Items)
        {
            vm.Directory?.SetPermissions(
                vm.User,
                vm.FileSystemRights);
        }

        LogItems.Add(LogItemFactory.CreateInformation("Folder permissions is ensured"));
    }

    protected string ResolveTemplateIfNeededByApplicationSettingsLookup(
        string value)
    {
        if (value.ContainsTemplateKeyBrackets())
        {
            var keys = value.GetTemplateKeys();
            foreach (var key in keys)
            {
                if (key.Contains('|', StringComparison.Ordinal))
                {
                    var sa = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (sa.Length == 2)
                    {
                        var refComponentProvider = refComponentProviders.First(x => x.Name == sa[0]);
                        if (refComponentProvider is not null &&
                            refComponentProvider.TryGetStringFromApplicationSetting(sa[1], out var resultValue))
                        {
                            value = resultValue;
                        }
                    }
                }
                else if (TryGetStringFromApplicationSettings(key, out var resultValue))
                {
                    value = value.ReplaceTemplateWithKey(key, resultValue);
                }
            }
        }

        return value;
    }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(HostingFramework)}: {HostingFramework}";

    private void HandleDependentServiceState(
        UpdateDependentServiceStateMessage obj)
    {
        var vm = DependentServices.FirstOrDefault(x => x.Name.Equals(obj.Name, StringComparison.Ordinal));
        if (vm is null)
        {
            return;
        }

        vm.InstallationState = obj.InstallationState;
        vm.RunningState = obj.RunningState;

        RaisePropertyChanged(nameof(DependentServicesIssueCount));
    }

    private void ResolveInstalledMainFile(
        ApplicationOption applicationOption)
    {
        if (InstallationFolderPath is null)
        {
            return;
        }

        InstalledMainFilePath = applicationOption switch
        {
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationFolderPath, $"{Name}.exe"),
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationFolderPath, $"{Name}.exe"),
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationFolderPath, "index.html"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationFolderPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationFolderPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationFolderPath, "index.html"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationFolderPath, $"{Name}.exe"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationFolderPath, $"{Name}.exe"),
            _ => InstalledMainFilePath,
        };
    }

    private void ResolveDirectoriesForFolderPermissions()
    {
        foreach (var vm in FolderPermissions.Items)
        {
            if (vm.Directory is null)
            {
                continue;
            }

            var folder = vm.Folder;

            if (folder.StartsWith(@".\", StringComparison.Ordinal))
            {
                folder = ResolvedVirtuelRootFolder(folder);
            }

            if (folder.ContainsTemplateKeyBrackets())
            {
                var keys = folder.GetTemplateKeys();
                foreach (var key in keys)
                {
                    if (TryGetStringFromApplicationSettings(key, out var resultValue))
                    {
                        folder = folder.ReplaceTemplateWithKey(key, resultValue);
                    }
                }
            }

            vm.Directory = new DirectoryInfo(folder);
        }
    }

    private void WorkOnAnalyzeAndUpdateStates()
    {
        InstallationState = ComponentInstallationState.Checking;

        InstallationPrerequisites.Clear();

        if (UnpackedZipFolderPath is null)
        {
            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            RunningState = ComponentRunningState.NotAvailable;
        }

        if (InstallationFolderPath is not null &&
            InstalledMainFilePath is not null)
        {
            if (File.Exists(InstalledMainFilePath))
            {
                InstallationState = ComponentInstallationState.Installed;
            }

            if (UnpackedZipFolderPath is not null)
            {
                if (ComponentType == ComponentType.InternetInformationService &&
                    HostingFramework == HostingFrameworkType.NodeJs)
                {
                    WorkOnAnalyzeAndUpdateStatesForNodeJsVersion();
                }
                else
                {
                    WorkOnAnalyzeAndUpdateStatesForDotNet();
                }
            }
        }
        else
        {
            InstallationState = ComponentInstallationState.NotInstalled;
            RunningState = ComponentRunningState.NotAvailable;
        }

        CheckPrerequisites();
        CheckServiceState();

        if (InstallationState == ComponentInstallationState.Checking)
        {
            InstallationState = ComponentInstallationState.NotInstalled;
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    private void WorkOnAnalyzeAndUpdateStatesForDotNet()
    {
        if (UnpackedZipFolderPath is null ||
            InstalledMainFilePath is null)
        {
            return;
        }

        var installationMainFile = Path.Combine(UnpackedZipFolderPath, $"{Name}.exe");
        if (!File.Exists(installationMainFile))
        {
            installationMainFile = Path.Combine(UnpackedZipFolderPath, $"{Name}.dll");
        }

        if (File.Exists(installationMainFile) &&
            File.Exists(InstalledMainFilePath))
        {
            Version? sourceVersion = null;
            var installationMainFileVersion = FileVersionInfo.GetVersionInfo(installationMainFile);
            if (installationMainFileVersion?.FileVersion != null)
            {
                sourceVersion = new Version(installationMainFileVersion.FileVersion);
                InstallationVersion = installationMainFileVersion.FileVersion;
            }

            Version? destinationVersion = null;
            var installedMainFileVersion = FileVersionInfo.GetVersionInfo(InstalledMainFilePath);
            if (installedMainFileVersion?.FileVersion is not null)
            {
                destinationVersion = new Version(installedMainFileVersion.FileVersion);
                InstalledVersion = installedMainFileVersion.FileVersion;
            }

            if (sourceVersion is not null &&
                destinationVersion is not null &&
                sourceVersion.IsNewerThan(destinationVersion))
            {
                InstallationState = ComponentInstallationState.InstalledWithOldVersion;
            }
        }
    }

    private void WorkOnAnalyzeAndUpdateStatesForNodeJsVersion()
    {
        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return;
        }

        string? sourceVersion = null;
        var installationVersionFile = Path.Combine(UnpackedZipFolderPath, "version.json");
        if (File.Exists(installationVersionFile))
        {
            var sourceDynamicJson = new DynamicJson(new FileInfo(installationVersionFile));
            var sourceValue = sourceDynamicJson.GetValue("VERSION");
            if (sourceValue is not null)
            {
                sourceVersion = sourceValue.ToString();
                InstallationVersion = sourceVersion;
            }
        }

        string? destinationVersion = null;
        var installedVersionFile = Path.Combine(InstallationFolderPath, "version.json");
        if (File.Exists(installedVersionFile))
        {
            var destinationDynamicJson = new DynamicJson(new FileInfo(installedVersionFile));
            var destinationValue = destinationDynamicJson.GetValue("VERSION");
            if (destinationValue is not null)
            {
                destinationVersion = destinationValue.ToString();
                InstalledVersion = destinationVersion;
            }
        }

        if (sourceVersion is not null &&
            destinationVersion is not null)
        {
            if (sourceVersion == destinationVersion)
            {
                InstallationState = ComponentInstallationState.Installed;
            }
            else
            {
                var sortedSet = new SortedSet<string>(StringComparer.Ordinal)
                    {
                        sourceVersion,
                        destinationVersion,
                    };

                InstallationState = destinationVersion == sortedSet.First()
                    ? ComponentInstallationState.InstalledWithOldVersion
                    : ComponentInstallationState.Installed;
            }
        }
    }
}