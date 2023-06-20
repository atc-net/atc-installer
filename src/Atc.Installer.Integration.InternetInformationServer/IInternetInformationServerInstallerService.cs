namespace Atc.Installer.Integration.InternetInformationServer;

public interface IInternetInformationServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetWwwRootPath();

    bool IsMicrosoftDonNetFramework48();

    bool IsMicrosoftDonNet7();

    bool IsNodeJs18();

    bool IsInstalledManagementConsole();

    bool IsComponentInstalledWebSockets();

    bool IsComponentInstalledMicrosoftNetAppHostPack7();

    bool IsComponentInstalledMicrosoftAspNetCoreModule2();

    bool IsComponentInstalledUrlRewriteModule2();

    string? ResolvedVirtuelRootFolder(
        string folder);

    ComponentRunningState GetApplicationPoolState(
        string applicationPoolName);

    ComponentRunningState GetWebsiteState(
        string websiteName);

    Task<bool> CreateApplicationPool(
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> CreateWebsite(
        string websiteName,
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        DirectoryInfo physicalPath,
        ushort port,
        string? hostName = null,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> CreateWebsite(
        string websiteName,
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        DirectoryInfo physicalPath,
        ushort httpPort,
        ushort? httpsPort,
        string? hostName,
        bool requireServerNameIndication,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

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