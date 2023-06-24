namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private readonly INetworkShellService networkShellService;
    private readonly ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders;
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;
    private string? installationFile;
    private string? unpackedZipFolderPath;
    private string? installationFolderPath;
    private string? installedMainFilePath;
    private string? installedVersion;
    private string? installationVersion;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            networkShellService = new NetworkShellService();
            refComponentProviders = new ObservableCollectionEx<ComponentProviderViewModel>();
            InstallationState = ComponentInstallationState.Checking;
            InstallerTempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "atc-installer"));
            InstallationDirectory = new DirectoryInfo(Path.Combine(InstallerTempDirectory.FullName, "InstallationFiles"));
            ProjectName = "MyProject";
            Name = "MyApp";
            InstallationFolderPath = @"C:\ProgramFiles\MyApp";
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        INetworkShellService networkShellService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        IDictionary<string, object> defaultApplicationSettings,
        ApplicationOption applicationOption)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviders);
        ArgumentException.ThrowIfNullOrEmpty(projectName);
        ArgumentNullException.ThrowIfNull(defaultApplicationSettings);
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));
        this.refComponentProviders = refComponentProviders;
        InstallerTempDirectory = installerTempDirectory;
        InstallationDirectory = installationDirectory;
        ProjectName = projectName;
        DefaultApplicationSettings.Populate(defaultApplicationSettings);
        ApplicationSettings.Populate(applicationOption.ApplicationSettings);
        FolderPermissions.Populate(applicationOption.FolderPermissions);
        ConfigurationSettingsFiles.Populate(applicationOption.ConfigurationSettingsFiles);
        Name = applicationOption.Name;
        ComponentType = applicationOption.ComponentType;
        HostingFramework = applicationOption.HostingFramework;
        IsService = applicationOption.ComponentType is ComponentType.PostgreSqlServer or ComponentType.InternetInformationService or ComponentType.WindowsService;
        InstallationFile = applicationOption.InstallationFile;
        InstallationFolderPath = applicationOption.InstallationPath;
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public DirectoryInfo InstallerTempDirectory { get; }

    public DirectoryInfo InstallationDirectory { get; }

    public string ProjectName { get; }

    public ApplicationSettingsViewModel DefaultApplicationSettings { get; } = new();

    public ApplicationSettingsViewModel ApplicationSettings { get; } = new();

    public FolderPermissionsViewModel FolderPermissions { get; } = new();

    public ConfigurationSettingsFilesViewModel ConfigurationSettingsFiles { get; } = new();

    public string Name { get; }

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; }

    public HostingFrameworkType HostingFramework { get; }

    public bool IsService { get; }

    public string? InstallationFile
    {
        get => installationFile;
        protected set
        {
            installationFile = value;
            RaisePropertyChanged();
        }
    }

    public string? UnpackedZipFolderPath
    {
        get => unpackedZipFolderPath;
        protected set
        {
            unpackedZipFolderPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstallationFolderPath
    {
        get => installationFolderPath;
        protected set
        {
            installationFolderPath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstalledMainFilePath
    {
        get => installedMainFilePath;
        protected set
        {
            installedMainFilePath = value;
            RaisePropertyChanged();
        }
    }

    public string? InstalledVersion
    {
        get => installedVersion;
        protected set
        {
            installedVersion = value;
            RaisePropertyChanged();
        }
    }

    public string? InstallationVersion
    {
        get => installationVersion;
        protected set
        {
            installationVersion = value;
            RaisePropertyChanged();
        }
    }

    public IDictionary<FileInfo, DynamicJson> ConfigurationJsonFiles { get; } = new Dictionary<FileInfo, DynamicJson>();

    public IDictionary<FileInfo, XmlDocument> ConfigurationXmlFiles { get; } = new Dictionary<FileInfo, XmlDocument>();

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
            if (installationState == value)
            {
                return;
            }

            installationState = value;
            RaisePropertyChanged();

            Messenger.Default.Send(
                new UpdateDependentServiceStateMessage(
                Name,
                InstallationState,
                RunningState));
        }
    }

    public ComponentRunningState RunningState
    {
        get => runningState;
        set
        {
            if (runningState == value)
            {
                return;
            }

            runningState = value;
            RaisePropertyChanged();

            Messenger.Default.Send(
                new UpdateDependentServiceStateMessage(
                    Name,
                    InstallationState,
                    RunningState));
        }
    }

    public ObservableCollectionEx<LogItem> LogItems { get; } = new();

    public ObservableCollectionEx<InstallationPrerequisiteViewModel> InstallationPrerequisites { get; } = new();

    public int InstallationPrerequisiteIssueCount
        => InstallationPrerequisites.Count(x => x.CategoryType == LogCategoryType.Error);

    public ObservableCollectionEx<DependentServiceViewModel> DependentServices { get; } = new();

    public ObservableCollectionEx<EndpointViewModel> Endpoints { get; } = new();

    public ObservableCollection<EndpointViewModel> BrowserLinkEndpoints
        => new(Endpoints.Where(x => x.EndpointType == ComponentEndpointType.BrowserLink));

    public int DependentServicesIssueCount
        => DependentServices.Count(x => x.RunningState != ComponentRunningState.Running);
}