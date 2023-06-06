namespace Atc.Installer.Wpf.ComponentProvider.WindowsApplication;

public class WindowsApplicationComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly WindowsApplicationInstallerService waInstallerService;

    public WindowsApplicationComponentProviderViewModel(
        string projectName,
        ApplicationOption applicationOption)
        : base(
            projectName,
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
}