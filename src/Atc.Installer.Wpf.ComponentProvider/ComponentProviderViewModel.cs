namespace Atc.Installer.Wpf.ComponentProvider;

public class ComponentProviderViewModel : ViewModelBase, IComponentProvider
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
            Name = "MyApp";
            InstallationPath = @"C:\ProgramFiles\MyApp";
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        ApplicationOption applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        Name = applicationOption.Name;
        HostingFramework = applicationOption.HostingFramework;
        InstallationPath = applicationOption.InstallationPath;
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public string Name { get; }

    public HostingFrameworkType HostingFramework { get; }

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

    public ObservableCollectionEx<InstallationPrerequisiteViewModel> InstallationPrerequisites { get; } = new();

    public ObservableCollectionEx<DependentServiceViewModel> DependentServices { get; } = new();

    public void PrepareInstallationFiles()
    {
        // TODO: Improve - appsettings
        var installationsBasePath = Assembly.GetEntryAssembly()!.Location.Split("src")[0];
        var installationsPath = Path.Combine(installationsBasePath, @"SampleData\SampleApplications\InstallationFiles");
        var files = Directory.EnumerateFiles(installationsPath).ToArray();

        // TODO: Improve
        InstallationFile = files.FirstOrDefault(x => x.Contains($"{Name}.zip", StringComparison.OrdinalIgnoreCase));

        if (InstallationFile is not null &&
            InstallationFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            UnpackedZipPath = Path.Combine(Path.Combine(Path.GetTempPath(), "AtcInstaller"), Name);
            if (Directory.Exists(UnpackedZipPath))
            {
                // TODO: Check existing...
            }
            else
            {
                Directory.CreateDirectory(UnpackedZipPath);
                ZipFile.ExtractToDirectory(InstallationFile, UnpackedZipPath, overwriteFiles: true);
            }
        }
    }

    public void StartChecking()
        => Task.Run(async () => await WorkOnStartChecking());

    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
    public virtual void CheckPrerequisites() { }

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
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, ".env"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, ".env"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            _ => InstalledMainFile,
        };
    }

    private async Task WorkOnStartChecking()
    {
        IsBusy = true;
        InstallationState = ComponentInstallationState.Checking;

        InstallationPrerequisites.Clear();

        // TODO: Improve - appsettings
        var installationsBasePath = Assembly.GetEntryAssembly()!.Location.Split("src")[0];
        var installationsPath = Path.Combine(installationsBasePath, @"SampleData\SampleApplications\InstallationFiles");

        if (InstallationFile is null)
        {
            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            RunningState = ComponentRunningState.NotAvailable;
        }
        else
        {
            if (InstalledMainFile is null || !File.Exists(InstalledMainFile))
            {
                InstallationState = ComponentInstallationState.NotInstalled;
                RunningState = ComponentRunningState.NotAvailable;

                CheckPrerequisites();
            }
            else
            {
                // TODO: Improve
                var installationMainFile = Path.Combine(installationsPath, $"{Name}\\{Name}.dll");

                var hasInstalledWithOldVersion = false;
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
                            hasInstalledWithOldVersion = true;
                        }
                    }
                }

                InstallationState = hasInstalledWithOldVersion
                    ? ComponentInstallationState.InstalledWithOldVersion
                    : ComponentInstallationState.InstalledWithNewestVersion;

                CheckPrerequisites();

                RunningState = ComponentRunningState.Checking;

                // TODO: Check runningState
                RunningState = ComponentRunningState.Running;
            }
        }

        IsBusy = false;
    }
}