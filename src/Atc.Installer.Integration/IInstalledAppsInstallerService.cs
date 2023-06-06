namespace Atc.Installer.Integration;

public interface IInstalledAppsInstallerService : IInstallerService
{
    bool IsMicrosoftDonNetFramework48();

    bool IsMicrosoftDonNet7();

    bool IsAppInstalledByDisplayName(string appDisplayName);
}