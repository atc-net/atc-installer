// ReSharper disable InvertIf
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;
    private string? installationFile;
    private string? unpackedZipPath;
    private string? installationPath;
    private string? installedMainFile;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            InstallationState = ComponentInstallationState.Checking;
            ProjectName = "MyProject";
            Name = "MyApp";
            InstallationPath = @"C:\ProgramFiles\MyApp";
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectName);
        ArgumentNullException.ThrowIfNull(defaultApplicationSettings);
        ArgumentNullException.ThrowIfNull(applicationOption);

        ProjectName = projectName;
        DefaultApplicationSettingsViewModel.Populate(defaultApplicationSettings);
        ApplicationSettingsViewModel.Populate(applicationOption.ApplicationSettings);
        FolderPermissionsViewModel.Populate(applicationOption.FolderPermissions);
        ConfigurationSettingsFiles = applicationOption.ConfigurationSettingsFiles;
        Name = applicationOption.Name;
        ComponentType = applicationOption.ComponentType;
        HostingFramework = applicationOption.HostingFramework;
        IsService = applicationOption.ComponentType is ComponentType.PostgreSqlServer or ComponentType.InternetInformationService or ComponentType.WindowsService;
        InstallationPath = applicationOption.InstallationPath;
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public string ProjectName { get; }

    public ApplicationSettingsViewModel DefaultApplicationSettingsViewModel { get; } = new();

    public ApplicationSettingsViewModel ApplicationSettingsViewModel { get; } = new();

    public FolderPermissionsViewModel FolderPermissionsViewModel { get; } = new();

    public IList<ConfigurationSettingsFileOption> ConfigurationSettingsFiles { get; } = new List<ConfigurationSettingsFileOption>();

    public string Name { get; }

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; }

    public HostingFrameworkType HostingFramework { get; }

    public bool IsService { get; }

    public string? InstallationFile
    {
        get => installationFile;
        private set
        {
            installationFile = value;
            RaisePropertyChanged();
        }
    }

    public string? UnpackedZipPath
    {
        get => unpackedZipPath;
        protected set
        {
            unpackedZipPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstallationPath
    {
        get => installationPath;
        protected set
        {
            installationPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstalledMainFile
    {
        get => installedMainFile;
        protected set
        {
            installedMainFile = value;
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

    public int DependentServicesIssueCount
        => DependentServices.Count(x => x.RunningState != ComponentRunningState.Running);

    public bool TryGetStringFromApplicationSettings(
        string key,
        out string value)
    {
        if (ApplicationSettingsViewModel.TryGetString(key, out value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return DefaultApplicationSettingsViewModel.TryGetString(key, out value) &&
               !string.IsNullOrWhiteSpace(value);
    }

    public bool TryGetUshortFromApplicationSettings(
        string key,
        out ushort value)
    {
        if (ApplicationSettingsViewModel.TryGetUshort(key, out value) &&
            value != default)
        {
            return true;
        }

        return DefaultApplicationSettingsViewModel.TryGetUshort(key, out value) &&
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
                throw new ArgumentOutOfRangeException(nameof(toastNotificationType), toastNotificationType, null);
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
        if (InstallationPath is null)
        {
            return;
        }

        switch (HostingFramework)
        {
            case HostingFrameworkType.DonNetFramework48:
            {
                if (InstalledMainFile is not null)
                {
                    var mainAppConfigFile = new FileInfo(InstalledMainFile + ".config");
                    if (mainAppConfigFile.Exists)
                    {
                        var xml = FileHelper.ReadAllText(mainAppConfigFile);
                        var xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(xml);
                        ConfigurationXmlFiles.Add(mainAppConfigFile, xmlDocument);
                    }
                }

                var appConfigFile = new FileInfo(Path.Combine(InstallationPath, "app.config"));
                if (appConfigFile.Exists)
                {
                    var xml = FileHelper.ReadAllText(appConfigFile);
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    ConfigurationXmlFiles.Add(appConfigFile, xmlDocument);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationPath, "web.config"));
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
                var appSettingsFile = new FileInfo(Path.Combine(InstallationPath, "appsettings.json"));
                if (appSettingsFile.Exists)
                {
                    var dynamicJson = new DynamicJson(appSettingsFile);
                    ConfigurationJsonFiles.Add(appSettingsFile, dynamicJson);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationPath, "web.config"));
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
                var envFile = new FileInfo(Path.Combine(InstallationPath, "env.json"));
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
        // TODO: Improve installationsPath
        var installationsPath = Path.Combine(Path.GetTempPath(), @$"atc-installer\{ProjectName}\Download");
        if (!Directory.Exists(installationsPath))
        {
            Directory.CreateDirectory(installationsPath);
        }

        var files = Directory.EnumerateFiles(installationsPath).ToArray();

        // TODO: Improve
        InstallationFile = files.FirstOrDefault(x => x.Contains($"{Name}.zip", StringComparison.OrdinalIgnoreCase));

        if (InstallationFile is null ||
            !InstallationFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        UnpackedZipPath = Path.Combine(Path.GetTempPath(), @$"atc-installer\{ProjectName}\Unpacked\{Name}");

        if (!unpackIfIfExist &&
            Directory.Exists(UnpackedZipPath))
        {
            return;
        }

        if (Directory.Exists(UnpackedZipPath))
        {
            Directory.Delete(UnpackedZipPath, recursive: true);
        }

        Directory.CreateDirectory(UnpackedZipPath);

        ZipFile.ExtractToDirectory(InstallationFile, UnpackedZipPath, overwriteFiles: true);

        var filesToDelete = Directory.GetFiles(UnpackedZipPath, "appsettings.*.json");
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
        if (InstallationPath is null)
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Backup files"));

        var timestamp = DateTime.Now.ToString("yyyyMMdd_hhmmss", GlobalizationConstants.EnglishCultureInfo);
        var backupFolder = Path.Combine(Path.GetTempPath(), @$"atc-installer\{ProjectName}\Backup\{Name}");
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var sourceAppSettingsFile = Path.Combine(InstallationPath, "env.json");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"env_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationPath, "appsettings.json");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"appsettings_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationPath, "web.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"web_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }

        sourceAppSettingsFile = Path.Combine(InstallationPath, $"{Name}.exe.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"{Name}.exe_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            LogItems.Add(LogItemFactory.CreateTrace($"Backup file: {destinationAppSettingsFile}"));
        }
    }

    protected void CopyUnpackedFiles()
    {
        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Copy files"));

        ResolveDirectoriesForFolderPermissions();

        var useDeleteBeforeCopy = !FolderPermissionsViewModel
            .Items
            .Any(x => x.Directory is not null &&
                      x.Directory.FullName.StartsWith(InstallationPath, StringComparison.OrdinalIgnoreCase));

        var directoryUnpackedZip = new DirectoryInfo(UnpackedZipPath);
        var directoryInstallation = new DirectoryInfo(InstallationPath);

        if (useDeleteBeforeCopy)
        {
            directoryUnpackedZip.CopyAll(directoryInstallation);
        }
        else
        {
            var excludeDirectories = FolderPermissionsViewModel
                .Items
                .Where(x => x.Directory is not null &&
                            x.Directory.FullName.StartsWith(InstallationPath, StringComparison.OrdinalIgnoreCase))
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

        foreach (var vm in FolderPermissionsViewModel.Items)
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
                if (TryGetStringFromApplicationSettings(key, out var resultValue))
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
        if (InstallationPath is null)
        {
            return;
        }

        InstalledMainFile = applicationOption switch
        {
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, "index.html"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, "index.html"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DonNetFramework48 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            _ => InstalledMainFile,
        };
    }

    private void ResolveDirectoriesForFolderPermissions()
    {
        foreach (var vm in FolderPermissionsViewModel.Items)
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

        // TODO: check for setup.exe or .msi file
        if (UnpackedZipPath is null)
        {
            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            RunningState = ComponentRunningState.NotAvailable;
        }

        if (InstallationPath is not null &&
            InstalledMainFile is not null &&
            File.Exists(InstalledMainFile))
        {
            InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

            // TODO: Improve
            if (UnpackedZipPath is not null)
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
    }

    private void WorkOnAnalyzeAndUpdateStatesForDotNet()
    {
        if (UnpackedZipPath is null ||
            InstalledMainFile is null)
        {
            return;
        }

        var installationMainFile = Path.Combine(UnpackedZipPath, $"{Name}.exe");
        if (!File.Exists(installationMainFile))
        {
            installationMainFile = Path.Combine(UnpackedZipPath, $"{Name}.dll");
        }

        if (File.Exists(installationMainFile))
        {
            var installationMainFileVersion = FileVersionInfo.GetVersionInfo(installationMainFile);
            var installedMainFileVersion = FileVersionInfo.GetVersionInfo(InstalledMainFile);
            if (installationMainFileVersion.FileVersion is not null &&
                installedMainFileVersion.FileVersion is not null)
            {
                var sourceVersion = new Version(installationMainFileVersion.FileVersion);
                var destinationVersion = new Version(installedMainFileVersion.FileVersion);

                if (sourceVersion.IsNewerThan(destinationVersion))
                {
                    InstallationState = ComponentInstallationState.InstalledWithOldVersion;
                }
            }
        }
    }

    private void WorkOnAnalyzeAndUpdateStatesForNodeJsVersion()
    {
        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return;
        }

        var installationVersionFile = Path.Combine(UnpackedZipPath, "version.json");
        var installedVersionFile = Path.Combine(InstallationPath, "version.json");
        if (File.Exists(installationVersionFile) &&
            File.Exists(installedVersionFile))
        {
            var sourceDynamicJson = new DynamicJson(new FileInfo(installationVersionFile));
            var sourceValue = sourceDynamicJson.GetValue("VERSION");
            var destinationDynamicJson = new DynamicJson(new FileInfo(installedVersionFile));
            var destinationValue = destinationDynamicJson.GetValue("VERSION");
            if (sourceValue is not null &&
                destinationValue is not null)
            {
                var sortedSet = new SortedSet<string>(StringComparer.Ordinal)
                {
                    sourceValue.ToString()!,
                    destinationValue.ToString()!,
                };

                if (sortedSet.Last() == sourceValue.ToString()!)
                {
                    InstallationState = ComponentInstallationState.InstalledWithOldVersion;
                }
            }
        }
    }
}