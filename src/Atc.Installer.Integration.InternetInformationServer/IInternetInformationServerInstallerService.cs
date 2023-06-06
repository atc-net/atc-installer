namespace Atc.Installer.Integration.InternetInformationServer;

public interface IInternetInformationServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetWwwRootPath();

    bool IsInstalledManagementConsole();

    bool IsComponentInstalledWebSockets();

    bool IsComponentInstalledMicrosoftNetAppHostPack7();

    bool IsComponentInstalledUrlRewriteModule2();

    ComponentRunningState GetApplicationPoolState(
        string applicationPoolName);

    ComponentRunningState GetWebsiteState(
        string websiteName);
}