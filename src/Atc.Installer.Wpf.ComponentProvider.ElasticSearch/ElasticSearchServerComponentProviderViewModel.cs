namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

public partial class ElasticSearchServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private string? testConnectionResult;

    public ElasticSearchServerComponentProviderViewModel(
        ILogger<ComponentProviderViewModel> logger,
        IElasticSearchServerInstallerService elasticSearchServerInstallerService,
        INetworkShellService networkShellService,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            logger,
            networkShellService,
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

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.PartiallyRunning &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        var isRunning = esInstallerService.IsRunning;
        RunningState = isRunning
            ? ComponentRunningState.Running
            : ComponentRunningState.Stopped;
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
        => RunningState == ComponentRunningState.Stopped;

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
        InstalledMainFilePath = esInstallerService.GetInstalledMainFile()?.FullName;
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