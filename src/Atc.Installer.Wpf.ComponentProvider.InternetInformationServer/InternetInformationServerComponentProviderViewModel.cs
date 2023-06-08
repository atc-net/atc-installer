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

        if (TryGetUshortFromApplicationSettings("http", out var httpValue))
        {
            Http = httpValue;
        }

        if (TryGetUshortFromApplicationSettings("https", out var httpsValue))
        {
            Https = httpsValue;
        }

        if (TryGetStringFromDefaultApplicationSettings("HostName", out var hostnameValue))
        {
            HostName = hostnameValue;
        }
    }

    public bool IsRequiredWebSockets { get; }

    public ushort? Http { get; }

    public ushort? Https { get; }

    public string? HostName { get; }

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
        if (UnpackedZipPath is null)
        {
            return false;
        }

        return RunningState switch
        {
            ComponentRunningState.Stopped => true,
            ComponentRunningState.Unknown when InstallationState == ComponentInstallationState.NotInstalled => true,
            _ => false,
        };
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK - for now.")]
    public override async Task ServiceDeployCommandHandler()
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
            LogItems.Add(LogItemFactory.CreateTrace("Create Website"));

            var isWebsiteCreated = await iisInstallerService
                .CreateWebsite(
                    Name,
                    Name,
                    new DirectoryInfo(InstallationPath),
                    Http.Value,
                    Https,
                    HostName,
                    requireServerNameIndication: true)
                .ConfigureAwait(true);

            if (isWebsiteCreated)
            {
                InstallationState = ComponentInstallationState.InstalledWithNewestVersion;
                LogItems.Add(LogItemFactory.CreateInformation("Website is created"));

                LogItems.Add(LogItemFactory.CreateTrace("Copy files"));
                CopyAll(UnpackedZipPath, InstallationPath);
                LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));

                RunningState = iisInstallerService.GetWebsiteState(Name);
                if (RunningState == ComponentRunningState.Stopped)
                {
                    var isWebsiteStarted = await iisInstallerService.StartWebsite(Name);
                    if (!isWebsiteStarted)
                    {
                        LogItems.Add(LogItemFactory.CreateWarning("Website have some problem with auto-start"));
                    }

                    RunningState = iisInstallerService.GetWebsiteState(Name);
                }

                isDone = true;
            }
            else
            {
                LogItems.Add(LogItemFactory.CreateError("Website is not created"));
            }
        }
        else if (RunningState == ComponentRunningState.Stopped &&
                 UnpackedZipPath is not null &&
                 InstallationPath is not null)
        {
            LogItems.Add(LogItemFactory.CreateTrace("Copy files"));
            CopyAll(UnpackedZipPath, InstallationPath);
            LogItems.Add(LogItemFactory.CreateInformation("Files is copied"));
            isDone = true;
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

    public static void CopyAll(
        string sourcePath,
        string destinationPath)
    {
        // Create the destination directory if it doesn't exist
        Directory.CreateDirectory(destinationPath);

        // Copy all files
        foreach (var sourceFile in Directory.GetFiles(sourcePath))
        {
            var fileName = Path.GetFileName(sourceFile);
            var destinationFile = Path.Combine(destinationPath, fileName);
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        // Recursively copy all subdirectories
        foreach (var sourceSubDirectory in Directory.GetDirectories(sourcePath))
        {
            var subDirectoryName = Path.GetFileName(sourceSubDirectory);
            var destinationSubDirectory = Path.Combine(destinationPath, subDirectoryName);
            CopyAll(sourceSubDirectory, destinationSubDirectory);
        }
    }
}