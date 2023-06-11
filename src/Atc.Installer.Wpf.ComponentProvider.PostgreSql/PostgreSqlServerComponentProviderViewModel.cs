namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

public partial class PostgreSqlServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly PostgreSqlServerInstallerService pgInstallerService;
    private string? testConnectionResult;

    public PostgreSqlServerComponentProviderViewModel(
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
        : base(
            projectName,
            defaultApplicationSettings,
            applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        pgInstallerService = PostgreSqlServerInstallerService.Instance;
        PostgreSqlConnectionViewModel = new PostgreSqlConnectionViewModel();

        InstalledMainFile = pgInstallerService.GetInstalledMainFile()?.FullName;

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

        if (TryGetStringFromApplicationSettings("Username", out var usernameValue))
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