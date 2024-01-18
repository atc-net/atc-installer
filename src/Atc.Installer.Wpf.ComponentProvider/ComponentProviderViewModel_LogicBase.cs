// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable InvertIf
// ReSharper disable StringLiteralTypo
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel
{
    public void SetFilterTextForMenu(
        string filterText)
    {
        FilterTextForMenu = filterText;
    }

    public bool IsKeyInDefaultApplicationSettings(
        string key)
        => DefaultApplicationSettings.Items.Any(item => item.Key.Equals(key, StringComparison.Ordinal));

    public bool IsKeyInApplicationSettings(
        string key)
        => ApplicationSettings.Items.Any(item => item.Key.Equals(key, StringComparison.Ordinal));

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
                AddLogItem(LogLevel.Information, message);
                break;
            case ToastNotificationType.Warning:
                AddLogItem(LogLevel.Warning, message);
                break;
            case ToastNotificationType.Error:
                AddLogItem(LogLevel.Error, message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(toastNotificationType), toastNotificationType, message: null);
        }

        Messenger.Default.Send(
            new ToastNotificationMessage(
                toastNotificationType,
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

        ConfigurationJsonFiles.Clear();
        ConfigurationXmlFiles.Clear();

        switch (HostingFramework)
        {
            case HostingFrameworkType.DonNetFramework48:
            {
                if (InstalledMainFilePath is not null)
                {
                    var mainAppConfigFile = new FileInfo(InstalledMainFilePath.GetValueAsString() + ".config");
                    if (mainAppConfigFile.Exists)
                    {
                        var xml = FileHelper.ReadAllText(mainAppConfigFile);
                        var xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(xml);
                        ConfigurationXmlFiles.Add(mainAppConfigFile, xmlDocument);
                    }
                }

                var appConfigFile = new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "app.config"));
                if (appConfigFile.Exists)
                {
                    var xml = FileHelper.ReadAllText(appConfigFile);
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xml);
                    ConfigurationXmlFiles.Add(appConfigFile, xmlDocument);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "web.config"));
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
            case HostingFrameworkType.DotNet8:
            {
                var appSettingsFile = IsDotNetBlazorWebAssembly()
                    ? new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "wwwroot", "appsettings.json"))
                    : new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "appsettings.json"));

                if (appSettingsFile.Exists)
                {
                    var dynamicJson = new DynamicJson(appSettingsFile);
                    ConfigurationJsonFiles.Add(appSettingsFile, dynamicJson);
                }

                var webConfigFile = new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "web.config"));
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
                var envFile = new FileInfo(Path.Combine(InstallationFolderPath.GetValueAsString(), "env.json"));
                if (envFile.Exists)
                {
                    var dynamicJson = new DynamicJson(envFile);
                    ConfigurationJsonFiles.Add(envFile, dynamicJson);
                }

                break;
            }
        }
    }

    public void PrepareInstallationFiles(
        bool unpackIfExist)
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

        if (Directory.Exists(UnpackedZipFolderPath) &&
            Directory.GetFiles(UnpackedZipFolderPath).Length == 0)
        {
            Directory.Delete(UnpackedZipFolderPath, recursive: true);
        }

        if (unpackIfExist ||
            !Directory.Exists(UnpackedZipFolderPath) ||
            DirectoryHelper.ExistsAndContainsNoFiles(UnpackedZipFolderPath))
        {
            if (Directory.Exists(UnpackedZipFolderPath))
            {
                Directory.Delete(UnpackedZipFolderPath, recursive: true);
            }

            Directory.CreateDirectory(UnpackedZipFolderPath);

            ZipFile.ExtractToDirectory(installationFilePath, UnpackedZipFolderPath, overwriteFiles: true);

            DeleteDevelopmentConfigurationFiles(new DirectoryInfo(UnpackedZipFolderPath));
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

    public virtual void CheckPrerequisitesState() { }

    public virtual void CheckServiceState() { }

    public virtual bool TryGetStringFromApplicationSetting(
        string key,
        out string resultValue)
    {
        if (TryGetStringFromApplicationSettings(key, out var value))
        {
            resultValue = value;
            return true;
        }

        resultValue = string.Empty;
        return false;
    }

    public virtual void UpdateConfigurationDynamicJson(
        string fileName,
        DynamicJson dynamicJson)
    { }

    public virtual string ResolvedVirtualRootFolder(
        string folder)
    {
        return string.Empty;
    }

    public virtual void UpdateConfigurationXmlDocument(
        string fileName,
        XmlDocument xmlDocument)
    { }

    protected void AddLogItem(
        LogLevel logLevel,
        string message)
    {
        var logMessage = $"Component: {Name} -> {message}";
        switch (logLevel)
        {
            case LogLevel.Trace:
                LogItems.Add(LogItemFactory.CreateTrace(message));
                Logger.Log(logLevel, logMessage);
                break;
            case LogLevel.Debug:
                LogItems.Add(LogItemFactory.CreateDebug(message));
                Logger.Log(logLevel, logMessage);
                break;
            case LogLevel.Information:
                LogItems.Add(LogItemFactory.CreateInformation(message));
                Logger.Log(logLevel, logMessage);
                break;
            case LogLevel.Warning:
                LogItems.Add(LogItemFactory.CreateWarning(message));
                Logger.Log(logLevel, logMessage);
                break;
            case LogLevel.Error:
                LogItems.Add(LogItemFactory.CreateError(message));
                Logger.Log(logLevel, logMessage);
                break;
            case LogLevel.Critical:
                LogItems.Add(LogItemFactory.CreateCritical(message));
                Logger.Log(logLevel, logMessage);
                break;
            default:
                throw new SwitchCaseDefaultException(logLevel);
        }
    }

    protected void BackupConfigurationFilesAndLog()
    {
        if (InstallationFolderPath is null)
        {
            return;
        }

        AddLogItem(LogLevel.Trace, "Backup files");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_hhmmss", GlobalizationConstants.EnglishCultureInfo);
        var backupFolder = Path.Combine(InstallerTempDirectory.FullName, @$"{ProjectName}\Backup\{Name}");
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var sourceAppSettingsFile = Path.Combine(InstallationFolderPath.GetValueAsString(), "env.json");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"env_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            AddLogItem(LogLevel.Trace, $"Backup file: {destinationAppSettingsFile}");
        }

        sourceAppSettingsFile = IsDotNetBlazorWebAssembly()
            ? Path.Combine(
                InstallationFolderPath.GetValueAsString(),
                "wwwroot",
                "appsettings.json")
            : Path.Combine(InstallationFolderPath.GetValueAsString(), "appsettings.json");

        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"appsettings_{timestamp}.json");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            AddLogItem(LogLevel.Trace, $"Backup file: {destinationAppSettingsFile}");
        }

        sourceAppSettingsFile = Path.Combine(InstallationFolderPath.GetValueAsString(), "web.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"web_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            AddLogItem(LogLevel.Trace, $"Backup file: {destinationAppSettingsFile}");
        }

        sourceAppSettingsFile = Path.Combine(InstallationFolderPath.GetValueAsString(), $"{Name}.exe.config");
        if (File.Exists(sourceAppSettingsFile))
        {
            var destinationAppSettingsFile = Path.Combine(backupFolder, $"{Name}.exe_{timestamp}.config");
            File.Copy(sourceAppSettingsFile, destinationAppSettingsFile, overwrite: true);
            AddLogItem(LogLevel.Trace, $"Backup file: {destinationAppSettingsFile}");
        }
    }

    protected void CopyUnpackedFiles()
    {
        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return;
        }

        AddLogItem(LogLevel.Trace, "Copy files");

        ResolveDirectoriesForFolderPermissions();

        var useDeleteBeforeCopy = !FolderPermissions
            .Items
            .Any(x => x.Directory is not null &&
                      x.Directory.FullName.StartsWith(InstallationFolderPath.GetValueAsString(), StringComparison.OrdinalIgnoreCase));

        var directoryUnpackedZip = new DirectoryInfo(UnpackedZipFolderPath);
        var directoryInstallation = new DirectoryInfo(InstallationFolderPath.GetValueAsString());

        if (useDeleteBeforeCopy)
        {
            directoryUnpackedZip.CopyAll(directoryInstallation);
        }
        else
        {
            var excludeDirectories = FolderPermissions
                .Items
                .Where(x => x.Directory is not null &&
                            x.Directory.FullName.StartsWith(InstallationFolderPath.GetValueAsString(), StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Directory!.Name)
                .ToList();

            directoryInstallation.DeleteAllFiles();
            directoryInstallation.DeleteAllDirectories("*", useRecursive: true, excludeDirectories);
            directoryUnpackedZip.CopyAll(directoryInstallation, useRecursive: true, deleteAllFromDestinationBeforeCopy: false);
        }

        DeleteDevelopmentConfigurationFiles(directoryInstallation);

        AddLogItem(LogLevel.Information, "Files is copied");
    }

    private static void DeleteDevelopmentConfigurationFiles(
        DirectoryInfo path)
    {
        if (path is null)
        {
            return;
        }

        var filesToDelete = Directory.GetFiles(path.FullName, "appsettings.*.json");
        foreach (var file in filesToDelete)
        {
            File.Delete(file);
        }
    }

    protected void UpdateConfigurationFiles()
    {
        AddLogItem(LogLevel.Trace, "Update configuration files");

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

        AddLogItem(LogLevel.Information, "Configuration files is updated copied");
    }

    protected void EnsureFolderPermissions()
    {
        AddLogItem(LogLevel.Trace, "Ensure folder permissions");

        ResolveDirectoriesForFolderPermissions();

        foreach (var vm in FolderPermissions.Items)
        {
            vm.Directory?.SetPermissions(
                vm.User,
                vm.FileSystemRights);
        }

        AddLogItem(LogLevel.Information, "Folder permissions is ensured");
    }

    protected void EnsureFirewallRules()
    {
        AddLogItem(LogLevel.Trace, "Ensure firewall rules");

        foreach (var vm in FirewallRules.Items)
        {
            if (windowsFirewallService.DoesRuleExist(vm.Name))
            {
                if (!windowsFirewallService.IsRuleEnabled(vm.Name))
                {
                    windowsFirewallService.EnableRule(vm.Name);
                }
            }
            else
            {
                switch (vm.Direction)
                {
                    case FirewallDirectionType.Inbound when vm.Protocol == FirewallProtocolType.Any:
                        windowsFirewallService.AddInboundRuleForAllowAny(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    case FirewallDirectionType.Inbound when vm.Protocol == FirewallProtocolType.Tcp:
                        windowsFirewallService.AddInboundRuleForAllowTcp(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    case FirewallDirectionType.Inbound when vm.Protocol == FirewallProtocolType.Udp:
                        windowsFirewallService.AddInboundRuleForAllowUdp(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    case FirewallDirectionType.Outbound when vm.Protocol == FirewallProtocolType.Any:
                        windowsFirewallService.AddOutboundRuleForAllowAny(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    case FirewallDirectionType.Outbound when vm.Protocol == FirewallProtocolType.Tcp:
                        windowsFirewallService.AddOutboundRuleForAllowTcp(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    case FirewallDirectionType.Outbound when vm.Protocol == FirewallProtocolType.Udp:
                        windowsFirewallService.AddOutboundRuleForAllowUdp(
                            vm.Name,
                            vm.Name,
                            vm.Port);
                        break;
                    default:
                        throw new SwitchCaseDefaultException(vm.Direction);
                }
            }
        }

        AddLogItem(LogLevel.Information, "Firewall rules is ensured");
    }

    protected string ResolveTemplateIfNeededByApplicationSettingsLookup(
        string value,
        int recursiveCallCount = 0)
    {
        if (value.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
        {
            var keys = value.GetTemplateKeys(TemplatePatternType.DoubleHardBrackets);
            foreach (var key in keys)
            {
                if (key.Contains('|', StringComparison.Ordinal))
                {
                    var sa = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (sa.Length == 2)
                    {
                        var refComponentProvider = RefComponentProviders.FirstOrDefault(x => x.Name.Equals(sa[0], StringComparison.OrdinalIgnoreCase));
                        if (refComponentProvider is not null &&
                            refComponentProvider.TryGetStringFromApplicationSetting(sa[1], out var resultValue))
                        {
                            value = value.ReplaceTemplateKeyWithValue(key, resultValue, TemplatePatternType.DoubleHardBrackets);
                        }
                    }
                }
                else if (TryGetStringFromApplicationSettings(key, out var resultValue))
                {
                    value = value.ReplaceTemplateKeyWithValue(key, resultValue, TemplatePatternType.DoubleHardBrackets);
                }
            }
        }

        if (recursiveCallCount <= 3 &&
            value.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
        {
            value = ResolveTemplateIfNeededByApplicationSettingsLookup(value, recursiveCallCount + 1);
        }

        return value;
    }

    protected void WorkOnAnalyzeAndUpdateStatesForVersion()
    {
        if (UnpackedZipFolderPath is not null)
        {
            InstallationVersion = null;
            InstalledVersion = null;

            if (ComponentType == ComponentType.InternetInformationService &&
                HostingFramework == HostingFrameworkType.NodeJs)
            {
                WorkOnAnalyzeAndUpdateStatesForNodeJsVersion();
            }
            else
            {
                WorkOnAnalyzeAndUpdateStatesForDotNetVersion();
            }

            Messenger.Default.Send(new RefreshSelectedComponentProviderMessage());
        }
    }

    protected bool IsDotNetBlazorWebAssembly()
    {
        if (UnpackedZipFolderPath is null &&
            InstalledMainFilePath is null)
        {
            return false;
        }

        if (UnpackedZipFolderPath is not null)
        {
            var path = Path.Combine(UnpackedZipFolderPath, "wwwroot", "_framework");
            return Directory.Exists(path);
        }

        if (InstalledMainFilePath is not null)
        {
            var tmpInstalledMainFilePath = InstalledMainFilePath.GetValueAsString();
            if (tmpInstalledMainFilePath.Contains("_framework", StringComparison.Ordinal))
            {
                return Directory.Exists(tmpInstalledMainFilePath);
            }

            var path = Path.Combine(
                tmpInstalledMainFilePath,
                "wwwroot",
                "_framework");

            return Directory.Exists(path);
        }

        return false;
    }

    protected void ApplyDependentServicesToRunningStateIssues()
    {
        foreach (var vm in DependentServices.Where(x => x.RunningState != ComponentRunningState.Running))
        {
            RunningStateIssues.Add(
                new RunningStateIssue
                {
                    Name = vm.Name,
                    InstallationState = vm.InstallationState,
                    RunningState = vm.RunningState,
                });
        }
    }

    public (string Value, IList<string> TemplateLocations) ResolveValueAndTemplateLocations(
        string value)
    {
        var templateLocations = new List<string>();
        var keys = value.GetTemplateKeys(TemplatePatternType.DoubleHardBrackets);
        foreach (var key in keys)
        {
            if (key.Contains('|', StringComparison.Ordinal))
            {
                var sa = key.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (sa.Length == 2)
                {
                    var refComponentProvider = RefComponentProviders.FirstOrDefault(x => x.Name.Equals(sa[0], StringComparison.OrdinalIgnoreCase));
                    if (refComponentProvider is not null &&
                        refComponentProvider.TryGetStringFromApplicationSetting(sa[1], out var resultValue))
                    {
                        templateLocations.Add(resultValue);
                        templateLocations.Add(sa[1]);
                        value = value.ReplaceTemplateKeyWithValue(key, resultValue, TemplatePatternType.DoubleHardBrackets);
                    }
                }
            }
            else if (TryGetStringFromApplicationSettings(key, out var resultValue))
            {
                var location = IsKeyInApplicationSettings(key)
                    ? nameof(ApplicationSettings)
                    : nameof(DefaultApplicationSettings);

                templateLocations.Add(location);

                value = value.ReplaceTemplateKeyWithValue(key, resultValue, TemplatePatternType.DoubleHardBrackets);
            }
        }

        return (value, templateLocations);
    }

    public void ClearAllIsDirty()
    {
        DefaultApplicationSettings.ClearAllIsDirty();
        ApplicationSettings.ClearAllIsDirty();
        FolderPermissions.ClearAllIsDirty();
        FirewallRules.ClearAllIsDirty();
        ConfigurationSettingsFiles.ClearAllIsDirty();
        IsDirty = false;
    }

    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    public async Task<ReportingData> GetReportingData()
    {
        IsBusy = true;

        var reportingData = new ReportingData(
            Name);

        if (InstalledMainFilePath is not null)
        {
            var mainFile = new FileInfo(InstalledMainFilePath.GetValueAsString());
            reportingData.InstalledMainFilePath = mainFile.FullName;
            var allFiles = FileHelper.GetFiles(mainFile.Directory!);

            foreach (var fileInfo in allFiles)
            {
                var reportedFile = new ReportingFile(fileInfo);

                switch (HostingFramework)
                {
                    case HostingFrameworkType.DonNetFramework48 or
                        HostingFrameworkType.DotNet7 or
                        HostingFrameworkType.DotNet8:
                    {
                        if (".exe".Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase) ||
                            ".dll".Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var assembly = AssemblyHelper.Load(fileInfo);
                                var assemblyInformation = AssemblyInformationFactory.Create(assembly);

                                reportedFile.Version = assemblyInformation.Version.ToString();
                                reportedFile.TargetFramework = assemblyInformation.TargetFrameworkDisplayName;
                                reportedFile.IsDebugBuild = assemblyInformation.IsDebugBuild.HasValue && assemblyInformation.IsDebugBuild.Value;

                                if (mainFile.FullName == reportedFile.FullName)
                                {
                                    reportingData.Version = assemblyInformation.Version.ToString();
                                }
                            }
                            catch (Exception)
                            {
                                // Skip
                            }
                        }

                        break;
                    }

                    case HostingFrameworkType.NodeJs:
                    {
                        if ("version.json".Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            var sourceDynamicJson = new DynamicJson(fileInfo);
                            var sourceValue = sourceDynamicJson.GetValue("VERSION");
                            if (sourceValue is not null)
                            {
                                reportedFile.Version = sourceValue.ToString();
                                reportingData.Version = sourceValue.ToString();
                            }
                        }

                        break;
                    }
                }

                reportingData.Files.Add(reportedFile);
            }
        }

        IsBusy = false;

        await Task.CompletedTask.ConfigureAwait(false);
        return reportingData;
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

    private void ResolveInstallationPathAndSetInstallationFolderPath(
        ApplicationOption applicationOption)
    {
        if (applicationOption.InstallationPath.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
        {
            var (resolvedValue, templateLocations) = ResolveValueAndTemplateLocations(applicationOption.InstallationPath);

            InstallationFolderPath = new ValueTemplateItemViewModel(
                resolvedValue,
                template: applicationOption.InstallationPath,
                templateLocations);
        }
        else
        {
            InstallationFolderPath = new ValueTemplateItemViewModel(
                applicationOption.InstallationPath,
                template: null,
                templateLocations: null);
        }
    }

    private void ResolveInstalledMainFile(
        ApplicationOption applicationOption)
    {
        if (InstallationFolderPath is null)
        {
            return;
        }

        var instFolderPath = GetInstallationFolderPathAsTemplateValue();

        string basePath;
        IList<string>? templateLocations = null;
        if (instFolderPath.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
        {
            var (resolvedValue, resolvedTemplateLocations) = ResolveValueAndTemplateLocations(instFolderPath);
            basePath = resolvedValue;
            templateLocations = resolvedTemplateLocations;
        }
        else
        {
            basePath = instFolderPath;
        }

        switch (applicationOption)
        {
            case { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DotNet7 }:
            case { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DotNet8 }:
            case { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DonNetFramework48 }:
                InstalledMainFilePath = new ValueTemplateItemViewModel(
                    Path.Combine(basePath, $"{Name}.exe"),
                    template: instFolderPath,
                    templateLocations);
                break;
            case { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.NodeJs }:
                InstalledMainFilePath = new ValueTemplateItemViewModel(
                    Path.Combine(basePath, "index.html"),
                    template: instFolderPath,
                    templateLocations);
                break;
            case { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 }:
            case { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet8 }:
            case { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DonNetFramework48 }:
                InstalledMainFilePath = new ValueTemplateItemViewModel(
                    Path.Combine(basePath, $"{Name}.dll"),
                    template: instFolderPath,
                    templateLocations);
                break;
            case { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.NodeJs }:
                InstalledMainFilePath = new ValueTemplateItemViewModel(
                    Path.Combine(basePath, "index.html"),
                    template: instFolderPath,
                    templateLocations);
                break;
            case { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet7 }:
            case { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet8 }:
            case { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DonNetFramework48 }:
                InstalledMainFilePath = new ValueTemplateItemViewModel(
                    Path.Combine(basePath, $"{Name}.exe"),
                    template: instFolderPath,
                    templateLocations);
                break;
        }
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
                folder = ResolvedVirtualRootFolder(folder);
            }

            folder = ResolveTemplateIfNeededByApplicationSettingsLookup(folder);

            vm.Directory = new DirectoryInfo(folder);
        }
    }

    private string GetInstallationFolderPathAsTemplateValue()
    {
        if (InstallationFolderPath is null)
        {
            return string.Empty;
        }

        var instFolderPath = InstallationFolderPath.GetValueAsString();

        var defaultTemplatePaths = DefaultApplicationSettings.Items
            .Where(x => x.GetValueAsString().Contains(":\\", StringComparison.Ordinal))
            .ToArray();

        if (defaultTemplatePaths.Length > 0)
        {
            foreach (var templatePath in defaultTemplatePaths)
            {
                if (instFolderPath.StartsWith(templatePath.GetValueAsString(), StringComparison.Ordinal))
                {
                    instFolderPath = instFolderPath.Replace(
                        templatePath.GetValueAsString(),
                        $"[[{templatePath.Key}]]",
                        StringComparison.Ordinal);
                    break;
                }
            }
        }

        return instFolderPath;
    }

    private string AdjustInstalledMainFilePathIfNeededAndGetInstallationMainPath()
    {
        if (UnpackedZipFolderPath is null ||
            InstalledMainFilePath is null)
        {
            return string.Empty;
        }

        if (IsDotNetBlazorWebAssembly())
        {
            var tmpInstalledMainFilePath = InstalledMainFilePath.GetValueAsString();
            if (!tmpInstalledMainFilePath.Contains("_framework", StringComparison.Ordinal))
            {
                var fileInfo = new FileInfo(tmpInstalledMainFilePath);

                InstalledMainFilePath.Value = HostingFramework switch
                {
                    HostingFrameworkType.DotNet7 => Path.Combine(
                        fileInfo.Directory!.FullName,
                        "wwwroot",
                        "_framework",
                        fileInfo.Name),
                    HostingFrameworkType.DotNet8 => Path.Combine(
                        fileInfo.Directory!.FullName,
                        "wwwroot",
                        "_framework",
                        fileInfo.Name.Replace(".dll", ".wasm", StringComparison.OrdinalIgnoreCase)),
                    _ => InstalledMainFilePath.Value,
                };

                return Path.Combine(
                    UnpackedZipFolderPath,
                    "wwwroot",
                    "_framework");
            }

            return tmpInstalledMainFilePath;
        }

        return UnpackedZipFolderPath;
    }

    private void WorkOnAnalyzeAndUpdateStates()
    {
        InstallationState = ComponentInstallationState.Checking;

        InstallationPrerequisites.Clear();

        if (!string.IsNullOrEmpty(RawInstallationPath) &&
            Directory.Exists(RawInstallationPath))
        {
            UnpackedZipFolderPath = RawInstallationPath;
        }

        if (UnpackedZipFolderPath is null)
        {
            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            RunningState = ComponentRunningState.NotAvailable;
        }

        if (InstallationFolderPath is not null &&
            InstalledMainFilePath is not null)
        {
            _ = AdjustInstalledMainFilePathIfNeededAndGetInstallationMainPath();
            if (File.Exists(InstalledMainFilePath.GetValueAsString()))
            {
                InstallationState = ComponentInstallationState.Installed;
            }

            WorkOnAnalyzeAndUpdateStatesForVersion();
        }
        else
        {
            InstallationState = ComponentInstallationState.NotInstalled;
            RunningState = ComponentRunningState.NotAvailable;
        }

        CheckPrerequisites();
        CheckPrerequisitesState();
        CheckServiceState();

        if (InstallationState == ComponentInstallationState.Checking)
        {
            InstallationState = ComponentInstallationState.NotInstalled;
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    private void WorkOnAnalyzeAndUpdateStatesForDotNetVersion()
    {
        if (UnpackedZipFolderPath is null ||
            InstalledMainFilePath is null)
        {
            return;
        }

        var resolvedInstalledMainFilePath = InstalledMainFilePath.GetValueAsString();
        if (!File.Exists(resolvedInstalledMainFilePath))
        {
            InstallationState = ComponentInstallationState.NotInstalled;
        }

        var installationMainFilePath = GetInstallationMainFilePath();

        if (installationMainFilePath is not null &&
            File.Exists(resolvedInstalledMainFilePath))
        {
            Version? sourceVersion = null;
            var installationMainFileVersion = FileVersionInfo.GetVersionInfo(installationMainFilePath);
            if (installationMainFileVersion?.FileVersion != null)
            {
                sourceVersion = new Version(installationMainFileVersion.FileVersion);
                InstallationVersion = installationMainFileVersion.FileVersion;
            }

            Version? destinationVersion = null;
            var installedMainFileVersion = FileVersionInfo.GetVersionInfo(resolvedInstalledMainFilePath);
            if (installedMainFileVersion?.FileVersion is not null)
            {
                destinationVersion = new Version(installedMainFileVersion.FileVersion);
                InstalledVersion = installedMainFileVersion.FileVersion;
            }

            if (VersionHelper.IsDefault(sourceVersion, destinationVersion))
            {
                var sourcePath = new DirectoryInfo(installationMainFilePath).Parent!;
                var destinationPath = new DirectoryInfo(resolvedInstalledMainFilePath).Parent!;
                if (sourcePath.GetTotalFilesLength("*.dll") != destinationPath.GetTotalFilesLength("*.dll"))
                {
                    InstallationState = ComponentInstallationState.InstalledWithOldVersion;
                }
            }
            else if (VersionHelper.IsSourceNewerThanDestination(sourceVersion, destinationVersion))
            {
                InstallationState = ComponentInstallationState.InstalledWithOldVersion;
            }
        }
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
    private void WorkOnAnalyzeAndUpdateStatesForNodeJsVersion()
    {
        if (UnpackedZipFolderPath is null ||
            InstallationFolderPath is null)
        {
            return;
        }

        if (!Directory.Exists(InstallationFolderPath.GetValueAsString()))
        {
            InstallationState = ComponentInstallationState.NotInstalled;
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
        var installedVersionFile = Path.Combine(InstallationFolderPath.GetValueAsString(), "version.json");
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
                InstallationState = VersionHelper.IsSourceNewerThanDestination(sourceVersion, destinationVersion)
                    ? ComponentInstallationState.InstalledWithOldVersion
                    : ComponentInstallationState.Installed;
            }
        }
    }

    private string? GetInstallationMainFilePath()
    {
        var installationMainPath = AdjustInstalledMainFilePathIfNeededAndGetInstallationMainPath();
        var installationMainFile = installationMainPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                                   installationMainPath.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase)
            ? installationMainPath
            : Path.Combine(installationMainPath, $"{Name}.exe");

        if (!File.Exists(installationMainFile))
        {
            installationMainFile = Path.Combine(installationMainPath, $"{Name}.dll");
        }

        return File.Exists(installationMainFile)
            ? installationMainFile
            : null;
    }
}