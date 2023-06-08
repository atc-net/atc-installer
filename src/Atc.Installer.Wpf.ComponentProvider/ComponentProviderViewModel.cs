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
        DefaultApplicationSettings = defaultApplicationSettings;
        ApplicationSettings = applicationOption.ApplicationSettings;
        Name = applicationOption.Name;
        HostingFramework = applicationOption.HostingFramework;
        IsService = applicationOption.ComponentType is ComponentType.InternetInformationService or ComponentType.WindowsService;
        InstallationPath = applicationOption.InstallationPath;
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public string ProjectName { get; }

    public IDictionary<string, object> DefaultApplicationSettings { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IDictionary<string, object> ApplicationSettings { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public string Name { get; }

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

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
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

    public ObservableCollectionEx<DependentServiceViewModel> DependentServices { get; } = new();

    public bool TryGetStringFromDefaultApplicationSettings(
        string key,
        out string value)
        => DefaultApplicationSettings.TryGetStringFromDictionary(key, out value);

    public bool TryGetUshortFromDefaultApplicationSettings(
        string key,
        out ushort value)
        => DefaultApplicationSettings.TryGetUshortFromDictionary(key, out value);

    public bool TryGetStringFromApplicationSettings(
        string key,
        out string value)
        => DefaultApplicationSettings.TryGetStringFromDictionary(key, out value);

    public bool TryGetUshortFromApplicationSettings(
        string key,
        out ushort value)
        => ApplicationSettings.TryGetUshortFromDictionary(key, out value);

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
    }

    public void AnalyzeAndUpdateStatesInBackgroundThread()
    {
        Task.Run(async () =>
        {
            IsBusy = true;

            await Task.Delay(3000);
            WorkOnAnalyzeAndUpdateStates();

            IsBusy = false;
        });
    }

    public virtual void CheckPrerequisites() { }

    public virtual void CheckServiceState() { }

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

    private void WorkOnAnalyzeAndUpdateStates()
    {
        InstallationState = ComponentInstallationState.Checking;

        InstallationPrerequisites.Clear();

        if (UnpackedZipPath is null) // TODO: check for setup.exe or .msi file
        {
            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            RunningState = ComponentRunningState.NotAvailable;
        }

        if (InstalledMainFile is not null &&
            File.Exists(InstalledMainFile))
        {
            InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

            if (UnpackedZipPath is not null) // TODO: Improve
            {
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
                        var a = new Version(installationMainFileVersion.FileVersion);
                        var b = new Version(installedMainFileVersion.FileVersion);

                        if (a.IsNewerThan(b))
                        {
                            InstallationState = ComponentInstallationState.InstalledWithOldVersion;
                        }
                    }
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
}