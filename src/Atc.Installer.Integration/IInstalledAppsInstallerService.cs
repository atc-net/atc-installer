namespace Atc.Installer.Integration;

public interface IInstalledAppsInstallerService
{
    bool IsAppInstalledByDisplayName(string appDisplayName);
}