namespace Atc.Installer.Integration.WindowsApplication;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
public sealed class WindowsApplicationInstallerService : IWindowsApplicationInstallerService
{
    private static readonly object InstanceLock = new();
    private static WindowsApplicationInstallerService? instance;

    private WindowsApplicationInstallerService()
    {
    }

    public static WindowsApplicationInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new WindowsApplicationInstallerService();
            }
        }
    }

    public bool IsMicrosoftDonNetFramework48()
        => InstalledAppsInstallerService.Instance.IsMicrosoftDonNetFramework48();

    public bool IsMicrosoftDonNet7()
        => InstalledAppsInstallerService.Instance.IsMicrosoftDonNet7();

    public ComponentRunningState GetRunningState(
        string serviceName)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service = services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null)
            {
                return ComponentRunningState.NotAvailable;
            }

            return service.Status switch
            {
                ServiceControllerStatus.ContinuePending => ComponentRunningState.Checking,
                ServiceControllerStatus.Paused => ComponentRunningState.Stopped,
                ServiceControllerStatus.PausePending => ComponentRunningState.Checking,
                ServiceControllerStatus.Running => ComponentRunningState.Running,
                ServiceControllerStatus.StartPending => ComponentRunningState.Checking,
                ServiceControllerStatus.Stopped => ComponentRunningState.Stopped,
                ServiceControllerStatus.StopPending => ComponentRunningState.Checking,
                _ => throw new SwitchExpressionException(service.Status),
            };
        }
        catch
        {
            return ComponentRunningState.Unknown;
        }
    }

    public async Task<bool> StopService(
        string serviceName,
        ushort timeoutInSeconds = 60)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service = services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null ||
                service.Status != ServiceControllerStatus.Running)
            {
                return false;
            }

            await Task
                .Run(() =>
                {
                    service.Stop();
                    service.WaitForStatus(
                        ServiceControllerStatus.Stopped,
                        TimeSpan.FromSeconds(timeoutInSeconds));
                })
                .ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StartService(
        string serviceName,
        ushort timeoutInSeconds = 60)
    {
        try
        {
            var services = ServiceController.GetServices();
            var service = services.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null ||
                service.Status != ServiceControllerStatus.Stopped)
            {
                return false;
            }

            await Task
                .Run(() =>
                {
                    service.Start();
                    service.WaitForStatus(
                        ServiceControllerStatus.Running,
                        TimeSpan.FromSeconds(timeoutInSeconds));
                })
                .ConfigureAwait(false);

            return true;
        }
        catch
        {
            return false;
        }
    }
}