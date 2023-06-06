// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly InternetInformationServerInstallerService iisInstallerService;

    public InternetInformationServerComponentProviderViewModel(
        string projectName,
        ApplicationOption applicationOption)
        : base(
            projectName,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

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

        IsRequiredWebSockets = applicationOption.DependentComponents.Contains("WebSockets", StringComparer.Ordinal);
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

    [SuppressMessage("SonarRules", "S3440:Variables should not be checked against the values they're about to be assigned", Justification = "OK.")]
    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!iisInstallerService.IsInstalled)
        {
            return;
        }

        RunningState = ComponentRunningState.Checking;
        var applicationPoolState = iisInstallerService.GetApplicationPoolState(Name);
        var websiteState = iisInstallerService.GetWebsiteState(Name);

        RunningState = applicationPoolState switch
        {
            ComponentRunningState.Running when websiteState == ComponentRunningState.Running => ComponentRunningState.Running,
            ComponentRunningState.Stopped when websiteState == ComponentRunningState.Stopped => ComponentRunningState.Stopped,
            _ => ComponentRunningState.Unknown,
        };

        if (RunningState == ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    public override bool CanServiceStopCommandHandler()
        => RunningState == ComponentRunningState.Running;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Stop service"));

        var isStopped = await iisInstallerService
            .StopWebsiteApplicationPool(Name, Name)
            .ConfigureAwait(true);

        if (isStopped)
        {
            RunningState = ComponentRunningState.Stopped;
            LogItems.Add(LogItemFactory.CreateInformation("Service is stopped"));
        }
        else
        {
            LogItems.Add(LogItemFactory.CreateError("Could not stop service"));
        }
    }

    public override bool CanServiceStartCommandHandler()
        => RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Start"));

        var isStarted = await iisInstallerService
            .StartWebsiteAndApplicationPool(Name, Name)
            .ConfigureAwait(true);

        if (isStarted)
        {
            RunningState = ComponentRunningState.Running;
            LogItems.Add(LogItemFactory.CreateInformation("Service is started"));
        }
        else
        {
            LogItems.Add(LogItemFactory.CreateError("Could not start service"));
        }
    }

    public override bool CanServiceDeployCommandHandler()
    {
        if (RunningState == ComponentRunningState.Stopped)
        {
            // TODO: Check installation data...
            return true;
        }

        return false;
    }

    public override Task ServiceDeployCommandHandler()
    {
        if (!CanServiceDeployCommandHandler())
        {
            return Task.CompletedTask;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Deploy"));

        // TODO:
        ////LogItems.Add(LogItemFactory.CreateTrace("Deployed"));
        return Task.CompletedTask;
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