namespace Atc.Installer.Integration.WindowsApplication;

public interface IWindowsApplicationInstallerService : IInstallerService
{
    bool IsMicrosoftDonNetFramework48();

    bool IsMicrosoftDonNet7();

    ComponentRunningState GetServiceState(
        string serviceName);

    Task<bool> StopService(
        string serviceName,
        ushort timeoutInSeconds = 60);

    Task<bool> StartService(
        string serviceName,
        ushort timeoutInSeconds = 60);
}