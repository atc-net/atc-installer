namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

public partial class ElasticSearchServerComponentProviderViewModel : ComponentProviderViewModel
{
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private string? testConnectionResult;

    public ElasticSearchServerComponentProviderViewModel(
        IElasticSearchServerInstallerService elasticSearchServerInstallerService,
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