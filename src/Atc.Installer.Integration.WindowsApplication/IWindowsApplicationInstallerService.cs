namespace Atc.Installer.Integration.WindowsApplication;

public interface IWindowsApplicationInstallerService : IInstallerService
{
    bool IsMicrosoftDotNetFramework48();

    bool IsMicrosoftDotNet7();

    bool IsMicrosoftDotNet8();

    bool IsComponentInstalledMicrosoftNetDesktop7();

    bool IsComponentInstalledMicrosoftNetDesktop8();

    ComponentRunningState GetServiceState(
        string serviceName);

    Task<bool> StopService(
        string serviceName,
        ushort timeoutInSeconds = 60);

    Task<bool> StartService(
        string serviceName,
        ushort timeoutInSeconds = 60);

    ComponentRunningState GetApplicationState(
        string applicationName);

    bool StopApplication(
        string applicationName,
        ushort timeoutInSeconds = 60);

    bool StopApplication(
        FileInfo applicationFile,
        ushort timeoutInSeconds = 60);

    bool StartApplication(
        FileInfo applicationFile,
        ushort timeoutInSeconds = 60);
}