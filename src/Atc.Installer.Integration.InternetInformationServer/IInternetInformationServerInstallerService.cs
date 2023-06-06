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

    Task<bool> StopApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> StopWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> StopWebsiteApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> StartApplicationPool(
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> StartWebsite(
        string websiteName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> StartWebsiteAndApplicationPool(
        string websiteName,
        string applicationPoolName,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);
}