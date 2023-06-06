namespace Atc.Installer.Integration.WindowsApplication;

public interface IWindowsApplicationInstallerService : IInstallerService
{
    ComponentRunningState GetRunningState(
        string serviceName);

    Task<bool> StopService(
        string serviceName,
        ushort timeoutInSeconds = 60);

    Task<bool> StartService(
        string serviceName,
        ushort timeoutInSeconds = 60);
}