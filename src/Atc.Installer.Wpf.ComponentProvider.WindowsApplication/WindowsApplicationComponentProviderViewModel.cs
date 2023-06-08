// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

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

        if (IsWindowsService)
        {
            RunningState = ComponentRunningState.Checking;
            RunningState = waInstallerService.GetServiceState(Name);
            if (RunningState == ComponentRunningState.Checking)
            {
                RunningState = ComponentRunningState.NotAvailable;
            }
        }
        else
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    public override bool CanServiceStopCommandHandler()
    {
        if (IsWindowsService)
        {
            return RunningState switch
            {
                ComponentRunningState.Running => true,
                _ => false,
            };
        }

        // TODO: Check application installation data / running process...
        return true;
    }

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Stop"));

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

        IsBusy = false;
    }

    public override bool CanServiceStartCommandHandler()
    {
        if (IsWindowsService)
        {
            return RunningState switch
            {
                ComponentRunningState.Stopped => true,
                _ => false,
            };
        }

        // TODO: Check application installation data / running process...
        return true;
    }

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Start"));

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

        IsBusy = false;
    }

    public override bool CanServiceDeployCommandHandler()
    {
        if (IsWindowsService)
        {
            return RunningState switch
            {
                ComponentRunningState.Stopped => true,
                ComponentRunningState.Unknown when InstallationState is ComponentInstallationState.NotInstalled or ComponentInstallationState.InstalledWithOldVersion => true,
                _ => false,
            };
        }

        // TODO: Check application installation data / running process...
        return true;
    }

    public override async Task ServiceDeployCommandHandler()
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
                isDone = await ServiceDeployWindowServiceCreate().ConfigureAwait(true);
            }
            else if (RunningState == ComponentRunningState.Stopped &&
                     UnpackedZipPath is not null &&
                     InstallationPath is not null)
            {
                isDone = ServiceDeployWindowServiceUpdate();
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

    private async Task<bool> ServiceDeployWindowServiceCreate()
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

        return isDone;
    }

    private bool ServiceDeployWindowServiceUpdate()
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

        return isDone;
    }

    private async Task<bool> ServiceDeployWindowApplicationCreate()
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

        return isDone;
    }

    private bool ServiceDeployWindowApplicationUpdate()
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

        return isDone;
    }
}