namespace Atc.Installer.Integration;

public interface IInstalledAppsInstallerService : IInstallerService
{
    bool IsMicrosoftDonNetFramework48();

    bool IsMicrosoftDonNet7();

    bool IsNodeJs18();

    bool IsJavaRuntime8();

    bool IsAppInstalledByDisplayName(
        string appDisplayName);

    Version? GetAppInstalledVersionByDisplayName(
        string appDisplayName);
}