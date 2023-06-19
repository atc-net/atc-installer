namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

public partial class ElasticSearchServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private string? testConnectionResult;

    public ElasticSearchServerComponentProviderViewModel(
        IElasticSearchServerInstallerService elasticSearchServerInstallerService,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            installerTempDirectory,
            installationDirectory,
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        esInstallerService = elasticSearchServerInstallerService ?? throw new ArgumentNullException(nameof(elasticSearchServerInstallerService));
        waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        ElasticSearchConnectionViewModel = new ElasticSearchConnectionViewModel();

        InstalledMainFilePath = esInstallerService.GetInstalledMainFile()?.FullName;
        ServiceName = esInstallerService.GetServiceName();

        IsRequiredJava = applicationOption.DependentComponents.Contains("Java", StringComparer.Ordinal);

        if (TryGetStringFromApplicationSettings("WebProtocol", out var webProtocolValue))
        {
            ElasticSearchConnectionViewModel.WebProtocol = ResolveTemplateIfNeededByApplicationSettingsLookup(webProtocolValue);
        }

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            ElasticSearchConnectionViewModel.HostName = ResolveTemplateIfNeededByApplicationSettingsLookup(hostNameValue);
        }

        if (TryGetUshortFromApplicationSettings("HostPort", out var hostPortValue))
        {
            ElasticSearchConnectionViewModel.HostPort = hostPortValue;
        }

        if (TryGetStringFromApplicationSettings("UserName", out var usernameValue))
        {
            ElasticSearchConnectionViewModel.Username = ResolveTemplateIfNeededByApplicationSettingsLookup(usernameValue);
        }

        if (TryGetStringFromApplicationSettings("Password", out var passwordValue))
        {
            ElasticSearchConnectionViewModel.Password = ResolveTemplateIfNeededByApplicationSettingsLookup(passwordValue);
        }

        if (TryGetStringFromApplicationSettings("Index", out var indexValue))
        {
            ElasticSearchConnectionViewModel.Index = ResolveTemplateIfNeededByApplicationSettingsLookup(indexValue);
        }

        TestConnectionResult = string.Empty;
    }

    public bool IsRequiredJava { get; }

    public ElasticSearchConnectionViewModel ElasticSearchConnectionViewModel { get; set; }

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

        LogItems.Add(LogItemFactory.CreateTrace("Stop"));

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

        LogItems.Add(LogItemFactory.CreateTrace("Start"));

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