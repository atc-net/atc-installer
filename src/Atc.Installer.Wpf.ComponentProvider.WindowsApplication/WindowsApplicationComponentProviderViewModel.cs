// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "OK.")]
public class WindowsApplicationComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly WindowsApplicationInstallerService waInstallerService;

    public WindowsApplicationComponentProviderViewModel(
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        waInstallerService = WindowsApplicationInstallerService.Instance;

        if (applicationOption.ComponentType == ComponentType.WindowsService)
        {
            IsWindowsService = true;
        }
    }

    public bool IsWindowsService { get; }

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        RunningState = IsWindowsService
            ? waInstallerService.GetServiceState(Name)
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
                .StopService(Name)
                .ConfigureAwait(true);

            if (isStopped)
            {
                RunningState = ComponentRunningState.Stopped;
                LogItems.Add(LogItemFactory.CreateInformation("Service is stopped"));
            }
            else
            {
                LogItems.Add(LogItemFactory.CreateError("Could not stop service"));
            }
        }
        else
        {
            var isStopped = waInstallerService
                .StopApplication(InstalledMainFile!);

            if (isStopped)
            {
                RunningState = ComponentRunningState.Stopped;
                LogItems.Add(LogItemFactory.CreateInformation("Application is stopped"));
            }
            else
            {
                LogItems.Add(LogItemFactory.CreateError("Could not stop application"));
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
                .StartService(Name)
                .ConfigureAwait(true);

            if (isStarted)
            {
                RunningState = ComponentRunningState.Running;
                LogItems.Add(LogItemFactory.CreateInformation("Service is started"));
            }
            else
            {
                LogItems.Add(LogItemFactory.CreateError("Could not start service"));
            }
        }
        else
        {
            var isStarted = waInstallerService
                .StartApplication(InstalledMainFile!);

            if (isStarted)
            {
                RunningState = ComponentRunningState.Running;
                LogItems.Add(LogItemFactory.CreateInformation("Application is started"));
            }
            else
            {
                LogItems.Add(LogItemFactory.CreateError("Could not start application"));
            }
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

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        CheckPrerequisitesForHostingFramework();
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
                UnpackedZipPath is not null &&
                InstallationPath is not null)
            {
                isDone = await ServiceDeployWindowServiceCreate(useAutoStart).ConfigureAwait(true);
            }
            else if (RunningState == ComponentRunningState.Stopped &&
                     UnpackedZipPath is not null &&
                     InstallationPath is not null)
            {
                isDone = await ServiceDeployWindowServiceUpdate(useAutoStart).ConfigureAwait(true);
            }
        }
        else
        {
            if (InstallationState == ComponentInstallationState.NotInstalled &&
                UnpackedZipPath is not null &&
                InstallationPath is not null)
            {
                isDone = await ServiceDeployWindowApplicationCreate().ConfigureAwait(true);
            }
            else if (UnpackedZipPath is not null &&
                     InstallationPath is not null)
            {
                isDone = ServiceDeployWindowApplicationUpdate();
            }
        }

        LogItems.Add(isDone
            ? LogItemFactory.CreateInformation("Deployed")
            : LogItemFactory.CreateError("Not deployed"));

        IsBusy = false;
    }

    private async Task<bool> ServiceDeployWindowServiceCreate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return isDone;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Copy files"));
        new DirectoryInfo(UnpackedZipPath).CopyAll(new DirectoryInfo(InstallationPath));
        LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));

        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting service"));
            await ServiceDeployWindowServiceStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Service is started")
                : LogItemFactory.CreateWarning("Service is not started"));
        }

        isDone = true;

        return isDone;
    }

    private async Task<bool> ServiceDeployWindowServiceUpdate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        CopyFilesAndLog();

        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting service"));
            await ServiceDeployWindowServiceStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Service is started")
                : LogItemFactory.CreateWarning("Service is not started"));
        }

        isDone = true;

        return isDone;
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

        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return Task.FromResult(isDone);
        }

        LogItems.Add(LogItemFactory.CreateTrace("Copy files"));
        new DirectoryInfo(UnpackedZipPath).CopyAll(new DirectoryInfo(InstallationPath));
        LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));

        isDone = true;

        return Task.FromResult(isDone);
    }

    private bool ServiceDeployWindowApplicationUpdate()
    {
        var isDone = false;

        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        CopyFilesAndLog();

        isDone = true;

        return isDone;
    }
}