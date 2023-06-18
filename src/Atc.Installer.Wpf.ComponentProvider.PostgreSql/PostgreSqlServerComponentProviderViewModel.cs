namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

public partial class PostgreSqlServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IPostgreSqlServerInstallerService pgInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private string? testConnectionResult;

    public PostgreSqlServerComponentProviderViewModel(
        IPostgreSqlServerInstallerService postgreSqlServerInstallerService,
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

        pgInstallerService = postgreSqlServerInstallerService ?? throw new ArgumentNullException(nameof(postgreSqlServerInstallerService));
        waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        PostgreSqlConnectionViewModel = new PostgreSqlConnectionViewModel();

        InstalledMainFilePath = pgInstallerService.GetInstalledMainFile()?.FullName;
        ServiceName = pgInstallerService.GetServiceName();

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            PostgreSqlConnectionViewModel.HostName = hostNameValue;
        }

        if (TryGetUshortFromApplicationSettings("HostPort", out var hostPortValue))
        {
            PostgreSqlConnectionViewModel.HostPort = hostPortValue;
        }

        if (TryGetStringFromApplicationSettings("Database", out var databaseValue))
        {
            PostgreSqlConnectionViewModel.Database = databaseValue;
        }

        if (TryGetStringFromApplicationSettings("UserName", out var usernameValue))
        {
            PostgreSqlConnectionViewModel.Username = usernameValue;
        }

        if (TryGetStringFromApplicationSettings("Password", out var passwordValue))
        {
            PostgreSqlConnectionViewModel.Password = passwordValue;
        }

        TestConnectionResult = string.Empty;
    }

    public PostgreSqlConnectionViewModel PostgreSqlConnectionViewModel { get; set; }

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

        if (pgInstallerService.IsInstalled)
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Information, "PostgreSQL is installed");
            if (pgInstallerService.IsRunning)
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Information, "PostgreSQL is running");
            }
            else
            {
                AddToInstallationPrerequisites("IsRunning", LogCategoryType.Error, "PostgreSQL is not running");
            }
        }
        else
        {
            AddToInstallationPrerequisites("IsInstalled", LogCategoryType.Error, "PostgreSQL is not installed");
        }

        InstallationPrerequisites.SuppressOnChangedNotification = false;
        RaisePropertyChanged(nameof(InstallationPrerequisites));
    }

    public override void CheckServiceState()
    {
        base.CheckServiceState();

        if (!pgInstallerService.IsInstalled)
        {
            return;
        }

        if (RunningState != ComponentRunningState.Stopped &&
            RunningState != ComponentRunningState.PartialRunning &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        var isRunning = pgInstallerService.IsRunning;
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
                $"PGSQL_{key}",
                categoryType,
                message));
    }
}