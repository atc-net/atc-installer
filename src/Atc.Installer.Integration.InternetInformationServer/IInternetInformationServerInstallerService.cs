namespace Atc.Installer.Integration.InternetInformationServer;

public interface IInternetInformationServerInstallerService : IInstallerService
{
    DirectoryInfo? GetWwwRootPath();

    bool IsInstalledManagementConsole();

    bool IsComponentInstalledWebSockets();

    bool IsComponentInstalledMicrosoftNetAppHostPack7();

    bool IsComponentInstalledUrlRewriteModule2();
}