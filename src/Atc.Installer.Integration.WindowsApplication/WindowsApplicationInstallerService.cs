namespace Atc.Installer.Integration.WindowsApplication;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Design", "CA1416:Validate platform compatibility", Justification = "OK.")]
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
}