// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly InternetInformationServerInstallerService iisInstallerService;

    public InternetInformationServerComponentProviderViewModel(
        ApplicationOption applicationOption)
        : base(applicationOption)
    {
        iisInstallerService = InternetInformationServerInstallerService.Instance;

        if (InstallationPath is not null)
        {
            InstallationPath = InternetInformationServerInstallerService
                .Instance
                .ResolvedVirtuelRootPath(InstallationPath);
        }

        if (InstalledMainFile is not null)
        {
            InstalledMainFile = InternetInformationServerInstallerService
                .Instance
                .ResolvedVirtuelRootPath(InstalledMainFile);
        }

        // TODO: IsRequiredWebSockets = applicationOption.InternetInformationServiceSettings-lookup
        IsRequiredWebSockets = true;
    }

    public bool IsRequiredWebSockets { get; }

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        if (iisInstallerService.IsInstalled)
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Information, "IIS is installed");
            if (iisInstallerService.IsRunning)
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Information, "IIS is running");
                CheckPrerequisitesForInstalled();
            }
            else
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Error, "IIS is not running");
            }
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Error, "IIS is not installed");
        }
    }

    private void CheckPrerequisitesForInstalled()
    {
        if (iisInstallerService.GetWwwRootPath() is null)
        {
            AddToInstallationPrerequisites("WwwRootPath", LogCategoryType.Error, "IIS wwwroot path is not found");
        }

        if (iisInstallerService.IsInstalledManagementConsole())
        {
            AddToInstallationPrerequisites("IsInstalledManagementConsole", LogCategoryType.Information, "IIS Management Console is installed");
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalledManagementConsole", LogCategoryType.Error, "IIS Management Console is not installed");
        }

        if (IsRequiredWebSockets)
        {
            if (iisInstallerService.IsComponentInstalledWebSockets())
            {
                AddToInstallationPrerequisites("IsComponentInstalledWebSockets", LogCategoryType.Information, "IIS WebSockets is installed");
            }
            else
            {
                AddToInstallationPrerequisites("IsComponentInstalledWebSockets", LogCategoryType.Warning, "IIS WebSockets is not installed");
            }
        }

        CheckPrerequisitesForHostingFramework();
    }

    private void CheckPrerequisitesForHostingFramework()
    {
        switch (HostingFramework)
        {
            case HostingFrameworkType.DotNet7:
                if (iisInstallerService.IsComponentInstalledMicrosoftNetAppHostPack7())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetAppHostPack7", LogCategoryType.Information, "IIS module 'Microsoft .NET AppHost Pack - 7' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledMicrosoftNetAppHostPack7", LogCategoryType.Warning, "IIS module 'Microsoft .NET AppHost Pack - 7' is not installed");
                }

                break;
            case HostingFrameworkType.NodeJs:
                if (iisInstallerService.IsComponentInstalledUrlRewriteModule2())
                {
                    AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Information, "IIS module 'URL Rewrite Module 2' is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsComponentInstalledUrlRewriteModule2", LogCategoryType.Warning, "IIS module 'URL Rewrite Module 2' is not installed");
                }

                break;
            default:
                throw new SwitchCaseDefaultException(HostingFramework);
        }
    }

    private void AddToInstallationPrerequisites(
        string key,
        LogCategoryType categoryType,
        string message)
    {
        InstallationPrerequisites.Add(
            new InstallationPrerequisiteViewModel(
                $"IIS_{key}",
                categoryType,
                message));
    }
}