namespace Atc.Installer.Wpf.ComponentProvider;

public class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            InstallationState = ComponentInstallationState.Checking;
            Name = "MyApp";
            InstallationPath = $"C:\\ProgramFiles\\MyApp";
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
        InstallationPath = applicationOption.InstallationPath;
        InstalledMainFile = applicationOption switch
        {
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            { ComponentType: ComponentType.Application, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, ".env"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.dll"),
            { ComponentType: ComponentType.InternetInformationService, HostingFramework: HostingFrameworkType.NodeJs } => Path.Combine(InstallationPath, ".env"),
            { ComponentType: ComponentType.WindowsService, HostingFramework: HostingFrameworkType.DotNet7 } => Path.Combine(InstallationPath, $"{Name}.exe"),
            _ => InstalledMainFile,
        };

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public string Name { get; }

    public string InstallationPath { get; }

    public string? InstallationFile { get; private set; }

    public string? InstalledMainFile { get; protected set; }

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

    public ObservableCollectionEx<DependentServiceViewModel> DependentServices { get; } = new();

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

    public void StartChecking()
        => Task.Run(async () => await WorkOnStartChecking());

    private async Task WorkOnStartChecking()
    {
        IsBusy = true;
        InstallationState = ComponentInstallationState.Checking;

        // TODO: Remove
        await Task.Delay(Random.Shared.Next(1000, 5000));

        // TODO: Improve - appsettings
        var installationsBasePath = Assembly.GetEntryAssembly()!.Location.Split("src")[0];
        var installationsPath = Path.Combine(installationsBasePath, @"SampleData\SampleApplications\InstallationFiles");
        var files = Directory.EnumerateFiles(installationsPath).ToArray();

        // TODO: Improve
        InstallationFile = files.FirstOrDefault(x => x.Contains($"{Name}.zip", StringComparison.OrdinalIgnoreCase));

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

                RunningState = ComponentRunningState.Checking;

                // TODO: Remove
                await Task.Delay(Random.Shared.Next(1000, 5000));

                // TODO: Check runningState
                RunningState = ComponentRunningState.Running;
            }
        }

        IsBusy = false;
    }
}