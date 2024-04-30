namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

public partial class PostgreSqlServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IPostgreSqlServerInstallerService pgInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private string? testConnectionResult;

    public PostgreSqlServerComponentProviderViewModel(
        ILoggerFactory loggerFactory,
        IPostgreSqlServerInstallerService postgreSqlServerInstallerService,
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

        pgInstallerService = postgreSqlServerInstallerService ?? throw new ArgumentNullException(nameof(postgreSqlServerInstallerService));
        waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        PostgreSqlConnection = new PostgreSqlConnectionViewModel();

        InitializeFromApplicationOptions();
    }

    private void InitializeFromApplicationOptions()
    {
        InstalledMainFilePath = new ValueTemplateItemViewModel(pgInstallerService.GetInstalledMainFile()?.FullName!, template: null, templateLocations: null);
        ServiceName = pgInstallerService.GetServiceName();

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            PostgreSqlConnection.HostName = ResolveTemplateIfNeededByApplicationSettingsLookup(hostNameValue);
        }

        if (TryGetUshortFromApplicationSettings("HostPort", out var hostPortValue))
        {
            PostgreSqlConnection.HostPort = hostPortValue;
        }

        if (TryGetStringFromApplicationSettings("Database", out var databaseValue))
        {
            PostgreSqlConnection.Database = ResolveTemplateIfNeededByApplicationSettingsLookup(databaseValue);
        }

        if (TryGetStringFromApplicationSettings("UserName", out var usernameValue))
        {
            PostgreSqlConnection.Username = ResolveTemplateIfNeededByApplicationSettingsLookup(usernameValue);
        }

        if (TryGetStringFromApplicationSettings("Password", out var passwordValue))
        {
            PostgreSqlConnection.Password = ResolveTemplateIfNeededByApplicationSettingsLookup(passwordValue);
        }

        TestConnectionResult = string.Empty;
    }

    public PostgreSqlConnectionViewModel PostgreSqlConnection { get; set; }

    public string? TestConnectionResult
    {
        get => testConnectionResult;
        set
        {
            testConnectionResult = value;
            RaisePropertyChanged();
        }
    }

    public override string Description => $"Database / {ComponentType.GetDescription()}";

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!pgInstallerService.IsInstalled)
        {
            return;
        }

        RunningState = pgInstallerService.IsRunning
            ? ComponentRunningState.Running
            : ComponentRunningState.Stopped;

        if (RunningState is ComponentRunningState.Unknown or ComponentRunningState.Checking)
        {
            RunningState = ComponentRunningState.NotAvailable;
        }
    }

    public override bool TryGetStringFromApplicationSetting(
        string key,
        out string resultValue)
    {
        if ("ConnectionString".Equals(key, StringComparison.Ordinal))
        {
            var connectionString = PostgreSqlConnection.GetConnectionString();
            if (connectionString is not null)
            {
                resultValue = connectionString;
                return true;
            }
        }

        resultValue = string.Empty;
        return false;
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
}