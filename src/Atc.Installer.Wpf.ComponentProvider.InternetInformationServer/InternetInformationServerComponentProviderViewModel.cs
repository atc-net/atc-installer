// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.ComponentProvider.InternetInformationServer;

public class InternetInformationServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly InternetInformationServerInstallerService iisInstallerService;

    public InternetInformationServerComponentProviderViewModel(
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        iisInstallerService = InternetInformationServerInstallerService.Instance;

        if (InstallationPath is not null)
        {
            InstallationPath = iisInstallerService.ResolvedVirtuelRootPath(InstallationPath);
        }

        if (InstalledMainFile is not null)
        {
            InstalledMainFile = iisInstallerService.ResolvedVirtuelRootPath(InstalledMainFile);
        }

        IsRequiredWebSockets = applicationOption.DependentComponents.Contains("WebSockets", StringComparer.Ordinal);

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            HostName = hostNameValue;
        }
        else if (TryGetStringFromDefaultApplicationSettings("HostName", out var hostNameDefaultValue))
        {
            HostName = hostNameDefaultValue;
        }

        if (TryGetUshortFromApplicationSettings("http", out var httpValue))
        {
            Http = httpValue;
        }

        if (TryGetUshortFromApplicationSettings("https", out var httpsValue))
        {
            Https = httpsValue;
        }
    }

    public bool IsRequiredWebSockets { get; }

    public string? HostName { get; }

    public ushort? Http { get; }

    public ushort? Https { get; }

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        InstallationPrerequisites.SuppressOnChangedNotification = true;

        if (iisInstallerService.IsInstalled)
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Information, "IIS is installed");
            if (iisInstallerService.IsRunning)
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Information, "IIS is running");
            }
            else
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Error, "IIS is not running");
            }

            CheckPrerequisitesForInstalled();
            CheckPrerequisitesForHostingFramework();
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Error, "IIS is not installed");
        }

        InstallationPrerequisites.SuppressOnChangedNotification = false;
        RaisePropertyChanged(nameof(InstallationPrerequisites));
    }

    [SuppressMessage("SonarRules", "S3440:Variables should not be checked against the values they're about to be assigned", Justification = "OK.")]
    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!iisInstallerService.IsInstalled)
        {
            return;
        }

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.PartialRunning &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        var applicationPoolState = iisInstallerService.GetApplicationPoolState(Name);
        var websiteState = iisInstallerService.GetWebsiteState(Name);

        RunningState = applicationPoolState switch
        {
            ComponentRunningState.Stopped when websiteState == ComponentRunningState.Stopped => ComponentRunningState.Stopped,
            ComponentRunningState.Running when websiteState == ComponentRunningState.Running => ComponentRunningState.Running,
            _ => applicationPoolState == ComponentRunningState.Running || websiteState == ComponentRunningState.Running
                ? ComponentRunningState.PartialRunning
                : ComponentRunningState.Unknown,
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

        IsBusy = true;

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

        IsBusy = false;
    }

    public override bool CanServiceStartCommandHandler()
        => RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

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

        IsBusy = false;
    }

    public override bool CanServiceDeployCommandHandler()
    {
        if (UnpackedZipPath is null)
        {
            return false;
        }

        return RunningState switch
        {
            ComponentRunningState.Stopped => true,
            ComponentRunningState.Unknown when InstallationState is ComponentInstallationState.NotInstalled or ComponentInstallationState.InstalledWithOldVersion => true,
            _ => false,
        };
    }

    public override Task ServiceDeployCommandHandler()
        => ServiceDeployAndStart(useAutoStart: false);

    public override Task ServiceDeployAndStartCommandHandler()
        => ServiceDeployAndStart(useAutoStart: true);

    private async Task ServiceDeployAndStart(
        bool useAutoStart)
    {
        if (!CanServiceDeployCommandHandler())
        {
            return;
        }

        IsBusy = true;

        LogItems.Add(LogItemFactory.CreateTrace("Deploy"));

        var isDone = false;

        if (InstallationState == ComponentInstallationState.NotInstalled &&
            UnpackedZipPath is not null &&
            InstallationPath is not null &&
            Http.HasValue)
        {
            isDone = await ServiceDeployWebsiteCreate(useAutoStart).ConfigureAwait(true);
        }
        else if (RunningState == ComponentRunningState.Stopped &&
                 UnpackedZipPath is not null &&
                 InstallationPath is not null)
        {
            isDone = await ServiceDeployWebsiteUpdate(useAutoStart).ConfigureAwait(true);
        }

        LogItems.Add(isDone
            ? LogItemFactory.CreateInformation("Deployed")
            : LogItemFactory.CreateError("Not deployed"));

        IsBusy = false;
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
    }

    private void CheckPrerequisitesForHostingFramework()
    {
        switch (HostingFramework)
        {
            case HostingFrameworkType.DonNetFramework48:
                if (iisInstallerService.IsMicrosoftDonNetFramework48())
                {
                    AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Information, "Microsoft .NET Framework 4.8 is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsMicrosoftDonNetFramework48", LogCategoryType.Warning, "Microsoft .NET Framework 4.8 is not installed");
                }

                break;

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

                if (iisInstallerService.IsNodeJs18())
                {
                    AddToInstallationPrerequisites("IsNodeJs", LogCategoryType.Information, "NodeJS is installed");
                }
                else
                {
                    AddToInstallationPrerequisites("IsNodeJs", LogCategoryType.Warning, "NodeJS is not installed");
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

    private async Task<bool> ServiceDeployWebsiteCreate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipPath is null ||
            InstallationPath is null ||
            Http is null)
        {
            return isDone;
        }

        LogItems.Add(LogItemFactory.CreateTrace("Create Website"));

        var isWebsiteCreated = await iisInstallerService
            .CreateWebsite(
                Name,
                Name,
                setApplicationPoolToUseDotNetClr: true,
                new DirectoryInfo(InstallationPath),
                Http.Value,
                Https,
                HostName,
                requireServerNameIndication: true)
            .ConfigureAwait(true);

        if (isWebsiteCreated)
        {
            LogItems.Add(LogItemFactory.CreateInformation("Website is created"));

            LogItems.Add(LogItemFactory.CreateTrace("Copy files"));
            new DirectoryInfo(UnpackedZipPath).CopyAll(new DirectoryInfo(InstallationPath));
            LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));

            InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

            if (useAutoStart)
            {
                LogItems.Add(LogItemFactory.CreateTrace("Auto starting website"));
                await ServiceDeployWebsiteStart().ConfigureAwait(true);
                LogItems.Add(RunningState == ComponentRunningState.Running
                    ? LogItemFactory.CreateInformation("Website is started")
                    : LogItemFactory.CreateWarning("Website is not started"));
            }

            isDone = true;
        }
        else
        {
            LogItems.Add(LogItemFactory.CreateError("Website is not created"));
        }

        return isDone;
    }

    private async Task<bool> ServiceDeployWebsiteUpdate(
        bool useAutoStart)
    {
        var isDone = false;

        if (UnpackedZipPath is null ||
            InstallationPath is null)
        {
            return isDone;
        }

        BackupConfigurationFilesAndLog();

        CopyFilesAndLog();

        InstallationState = ComponentInstallationState.InstalledWithNewestVersion;

        if (useAutoStart)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Auto starting website"));
            await ServiceDeployWebsiteStart().ConfigureAwait(true);
            LogItems.Add(RunningState == ComponentRunningState.Running
                ? LogItemFactory.CreateInformation("Website is started")
                : LogItemFactory.CreateWarning("Website is not started"));
        }

        isDone = true;

        return isDone;
    }

    private async Task ServiceDeployWebsiteStart()
    {
        await Task
            .Delay(100)
            .ConfigureAwait(false);

        RunningState = iisInstallerService.GetWebsiteState(Name);

        if (RunningState == ComponentRunningState.Stopped)
        {
            var isWebsiteStarted = await iisInstallerService
                .StartWebsite(Name)
                .ConfigureAwait(true);

            if (!isWebsiteStarted)
            {
                LogItems.Add(LogItemFactory.CreateWarning("Website have some problem with startup"));
            }

            RunningState = iisInstallerService.GetWebsiteState(Name);
        }
    }
}