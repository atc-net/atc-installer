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
            RunningState = waInstallerService.GetRunningState(Name);
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
        => IsWindowsService &&
           RunningState == ComponentRunningState.Running;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Stop"));
        RunningState = ComponentRunningState.Checking;

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

    public override bool CanServiceStartCommandHandler()
        => IsWindowsService &&
           RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Start"));
        RunningState = ComponentRunningState.Checking;

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

    public override bool CanServiceDeployCommandHandler()
    {
        if (IsWindowsService &&
            RunningState == ComponentRunningState.Stopped)
        {
            // TODO: Check installation data...
            return true;
        }

        if (!IsWindowsService)
        {
            // TODO: Check installation data...
            return true;
        }

        return false;
    }

    public override Task ServiceDeployCommandHandler()
    {
        if (!CanServiceDeployCommandHandler())
        {
            return Task.CompletedTask;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Deploy"));

        // TODO:
        ////LogItems.Add(LogItemFactory.CreateTrace("Deployed"));
        return Task.CompletedTask;
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
}