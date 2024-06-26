namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

public partial class ElasticSearchServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private string? testConnectionResult;

    public ElasticSearchServerComponentProviderViewModel(
        ILoggerFactory loggerFactory,
        IElasticSearchServerInstallerService elasticSearchServerInstallerService,
        INetworkShellService networkShellService,
        IWindowsFirewallService windowsFirewallService,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        ObservableCollectionEx<KeyValueTemplateItemViewModel> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            loggerFactory,
            networkShellService,
            windowsFirewallService,
            refComponentProviders,
            installerTempDirectory,
            installationDirectory,
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        esInstallerService = elasticSearchServerInstallerService ?? throw new ArgumentNullException(nameof(elasticSearchServerInstallerService));
        waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        ElasticSearchConnection = new ElasticSearchConnectionViewModel();

        InitializeFromApplicationOptions(applicationOption);
    }

    public bool IsRequiredJava { get; private set; }

    public ElasticSearchConnectionViewModel ElasticSearchConnection { get; set; }

    public string? TestConnectionResult
    {
        get => testConnectionResult;
        set
        {
            testConnectionResult = value;
            RaisePropertyChanged();
        }
    }

    public override string Description => ComponentType.GetDescription();

    public override void CheckPrerequisites()
    {
        base.CheckPrerequisites();

        InstallationPrerequisites.SuppressOnChangedNotification = true;

        if (IsRequiredJava)
        {
            if (esInstallerService.IsComponentInstalledJava())
            {
                AddToInstallationPrerequisites("IsComponentInstalledJava", LogCategoryType.Information, "Java is installed");
            }
            else
            {
                AddToInstallationPrerequisites("IsComponentInstalledJava", LogCategoryType.Warning, "Java is not installed");
            }
        }

        InstallationPrerequisites.SuppressOnChangedNotification = false;
        RaisePropertyChanged(nameof(InstallationPrerequisites));
    }

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!esInstallerService.IsInstalled)
        {
            return;
        }

        RunningState = esInstallerService.IsRunning
            ? ComponentRunningState.Running
            : ComponentRunningState.Stopped;

        if (RunningState is ComponentRunningState.Unknown or ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    public override bool CanServiceStopCommandHandler()
        => !DisableInstallationActions &&
           !HideMenuItem &&
           RunningState == ComponentRunningState.Running;

    public override async Task ServiceStopCommandHandler()
    {
        if (!CanServiceStopCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Stop service");

        var isStopped = await waInstallerService
            .StopService(ServiceName!)
            .ConfigureAwait(true);

        if (isStopped)
        {
            RunningState = ComponentRunningState.Stopped;
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Service is stopped");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Could not stop service");
        }

        IsBusy = false;
    }

    public override bool CanServiceStartCommandHandler()
        => !DisableInstallationActions &&
           !HideMenuItem &&
           RunningState == ComponentRunningState.Stopped;

    public override async Task ServiceStartCommandHandler()
    {
        if (!CanServiceStartCommandHandler())
        {
            return;
        }

        IsBusy = true;

        AddLogItem(LogLevel.Trace, "Start service");

        var isStarted = await waInstallerService
            .StartService(ServiceName!)
            .ConfigureAwait(true);

        if (isStarted)
        {
            RunningState = ComponentRunningState.Running;
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Information,
                Name,
                "Service is started");
        }
        else
        {
            LogAndSendToastNotificationMessage(
                ToastNotificationType.Error,
                Name,
                "Could not start service");
        }

        IsBusy = false;
    }

    private void InitializeFromApplicationOptions(ApplicationOption applicationOption)
    {
        InstalledMainFilePath = new ValueTemplateItemViewModel(esInstallerService.GetInstalledMainFile()?.FullName!, template: null, templateLocations: null);
        ServiceName = esInstallerService.GetServiceName();

        IsRequiredJava = applicationOption.DependentComponents.Contains("Java", StringComparer.Ordinal);

        if (TryGetStringFromApplicationSettings("WebProtocol", out var webProtocolValue))
        {
            ElasticSearchConnection.WebProtocol = ResolveTemplateIfNeededByApplicationSettingsLookup(webProtocolValue);
        }

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            ElasticSearchConnection.HostName = ResolveTemplateIfNeededByApplicationSettingsLookup(hostNameValue);
        }

        if (TryGetUshortFromApplicationSettings("HostPort", out var hostPortValue))
        {
            ElasticSearchConnection.HostPort = hostPortValue;
        }

        if (TryGetStringFromApplicationSettings("UserName", out var usernameValue))
        {
            ElasticSearchConnection.Username = ResolveTemplateIfNeededByApplicationSettingsLookup(usernameValue);
        }

        if (TryGetStringFromApplicationSettings("Password", out var passwordValue))
        {
            ElasticSearchConnection.Password = ResolveTemplateIfNeededByApplicationSettingsLookup(passwordValue);
        }

        if (TryGetStringFromApplicationSettings("Index", out var indexValue))
        {
            ElasticSearchConnection.Index = ResolveTemplateIfNeededByApplicationSettingsLookup(indexValue);
        }

        TestConnectionResult = string.Empty;
    }

    private void AddToInstallationPrerequisites(
        string key,
        LogCategoryType categoryType,
        string message)
    {
        InstallationPrerequisites.Add(
            new InstallationPrerequisiteViewModel(
                $"ES_{key}",
                categoryType,
                message));
    }
}