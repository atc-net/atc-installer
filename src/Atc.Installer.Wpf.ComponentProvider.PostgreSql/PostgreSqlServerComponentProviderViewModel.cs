namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

public class PostgreSqlServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly PostgreSqlServerInstallerService pgInstallerService;

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

        InstalledMainFile = pgInstallerService.GetInstalledMainFile()?.FullName;

        if (TryGetStringFromApplicationSettings("HostName", out var hostNameValue))
        {
            HostName = hostNameValue;
        }
        else if (TryGetStringFromDefaultApplicationSettings("HostName", out var hostNameDefaultValue))
        {
            HostName = hostNameDefaultValue;
        }

        if (TryGetUshortFromApplicationSettings("HostPort", out var hostPortValue))
        {
            HostPort = hostPortValue;
        }

        if (TryGetStringFromApplicationSettings("Database", out var databaseValue))
        {
            Database = databaseValue;
        }

        if (TryGetStringFromApplicationSettings("Username", out var usernameValue))
        {
            Username = usernameValue;
        }
        else if (TryGetStringFromDefaultApplicationSettings("Username", out var usernameDefaultValue))
        {
            Username = usernameDefaultValue;
        }

        if (TryGetStringFromApplicationSettings("Password", out var passwordValue))
        {
            Password = passwordValue;
        }
        else if (TryGetStringFromDefaultApplicationSettings("Password", out var passwordDefaultValue))
        {
            Password = passwordDefaultValue;
        }
    }

    public string? HostName { get; }

    public ushort? HostPort { get; }

    public string? Database { get; }

    public string? Username { get; }

    public string? Password { get; }

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