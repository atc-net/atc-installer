namespace Atc.Installer.Integration.WindowsApplication;

public interface IWindowsApplicationInstallerService : IInstallerService
{
    ComponentRunningState GetRunningState(
        string serviceName);
}