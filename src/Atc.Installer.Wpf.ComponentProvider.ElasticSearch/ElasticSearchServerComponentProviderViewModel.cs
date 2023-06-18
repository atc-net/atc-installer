namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

public class ElasticSearchServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;

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

        InstalledMainFilePath = esInstallerService.GetInstalledMainFile()?.FullName;
        ServiceName = esInstallerService.GetServiceName();

        IsRequiredJava = applicationOption.DependentComponents.Contains("Java", StringComparer.Ordinal);
    }

    public bool IsRequiredJava { get; }

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
            RunningState != ComponentRunningState.PartialRunning &&
            RunningState != ComponentRunningState.Running)
        {
            RunningState = ComponentRunningState.Checking;
        }

        var isRunning = esInstallerService.IsRunning;
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
                $"ES_{key}",
                categoryType,
                message));
    }
}