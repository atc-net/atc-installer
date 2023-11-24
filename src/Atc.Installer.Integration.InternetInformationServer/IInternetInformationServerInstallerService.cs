namespace Atc.Installer.Integration.InternetInformationServer;

public interface IInternetInformationServerInstallerService : IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }

    DirectoryInfo? GetWwwRootPath();

    bool IsMicrosoftDotNetFramework48();

    bool IsMicrosoftDotNet7();

    bool IsNodeJs18();

    bool IsInstalledManagementConsole();

    bool IsComponentInstalledWebSockets();

    bool IsComponentInstalledMicrosoftNetHost7();

    bool IsComponentInstalledMicrosoftNetHost8();

    bool IsComponentInstalledMicrosoftAspNetCoreModule2();

    bool IsComponentInstalledUrlRewriteModule2();

    string? ResolvedVirtualRootFolder(
        string folder);

    Task<(bool IsSucceeded, string? ErrorMessage)> UnlockConfigSectionSystemWebServerModules();

    Task<(bool IsSucceeded, string? ErrorMessage)> EnsureSettingsForComponentUrlRewriteModule2(
        DirectoryInfo installationPath);

    ComponentRunningState GetApplicationPoolState(
        string applicationPoolName);

    ComponentRunningState GetWebsiteState(
        string websiteName);

    Task<bool> CreateApplicationPool(
        string applicationPoolName,
        bool setApplicationPoolToUseDotNetClr,
        ushort timeoutInSeconds = 60,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteApplicationPool(
        string applicationPoolName,
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

    Task<bool> DeleteWebsite(
        string websiteName,
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

    IList<X509Certificate2> GetX509Certificates();

    X509Certificate2? GetWebsiteX509Certificate(
        string websiteName);

    bool AssignX509CertificateToWebsite(
        string websiteName,
        X509Certificate2 certificate);

    bool UnAssignX509CertificateOnWebsite(
        string websiteName);
}