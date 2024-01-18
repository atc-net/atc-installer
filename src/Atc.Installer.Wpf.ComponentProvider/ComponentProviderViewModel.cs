namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private readonly INetworkShellService networkShellService;
    private readonly IWindowsFirewallService windowsFirewallService;
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;
    private string? rawInstallationPath;
    private string? installationFile;
    private string? unpackedZipFolderPath;
    private ValueTemplateItemViewModel? installationFolderPath;
    private ValueTemplateItemViewModel? installedMainFilePath;
    private string? installedVersion;
    private string? installationVersion;
    private string filterTextForMenu = string.Empty;
    private bool showOnlyBaseSettings;
    private bool compactMode;
    private bool hideMenuItem;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            Logger = NullLogger<ComponentProviderViewModel>.Instance;
            networkShellService = new NetworkShellService();
            windowsFirewallService = new WindowsFirewallService();
            RefComponentProviders = new ObservableCollectionEx<ComponentProviderViewModel>();
            InstallationState = ComponentInstallationState.Checking;
            InstallerTempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "atc-installer"));
            InstallationDirectory = new DirectoryInfo(Path.Combine(InstallerTempDirectory.FullName, "InstallationFiles"));
            ProjectName = "MyProject";
            Name = "MyApp";
            InstallationFolderPath = new ValueTemplateItemViewModel(@"C:\ProgramFiles\MyApp", template: null, templateLocations: null);
            DefaultApplicationSettings = new ApplicationSettingsViewModel(isDefaultApplicationSettings: true, RefComponentProviders);
            ApplicationSettings = new ApplicationSettingsViewModel(isDefaultApplicationSettings: false, RefComponentProviders);
            FolderPermissions = new FolderPermissionsViewModel();
            FirewallRules = new FirewallRulesViewModel();
            ConfigurationSettingsFiles = new ConfigurationSettingsFilesViewModel();
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        ILogger<ComponentProviderViewModel> logger,
        INetworkShellService networkShellService,
        IWindowsFirewallService windowsFirewallService,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders,
        DirectoryInfo installerTempDirectory,
        DirectoryInfo installationDirectory,
        string projectName,
        ObservableCollectionEx<KeyValueTemplateItemViewModel> defaultApplicationSettings,
        ApplicationOption applicationOption)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviders);
        ArgumentException.ThrowIfNullOrEmpty(projectName);
        ArgumentNullException.ThrowIfNull(defaultApplicationSettings);
        ArgumentNullException.ThrowIfNull(applicationOption);

        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));
        this.windowsFirewallService = windowsFirewallService ?? throw new ArgumentNullException(nameof(windowsFirewallService));
        this.RefComponentProviders = refComponentProviders;
        InstallerTempDirectory = installerTempDirectory;
        InstallationDirectory = installationDirectory;
        ProjectName = projectName;
        Name = applicationOption.Name;
        DisableInstallationActions = applicationOption.DisableInstallationActions;

        DefaultApplicationSettings = new ApplicationSettingsViewModel(isDefaultApplicationSettings: true, refComponentProviders);
        DefaultApplicationSettings.Populate(defaultApplicationSettings);

        ApplicationSettings = new ApplicationSettingsViewModel(isDefaultApplicationSettings: false, refComponentProviders);
        ApplicationSettings.Populate(DefaultApplicationSettings, applicationOption.ApplicationSettings);

        FolderPermissions = new FolderPermissionsViewModel();
        FolderPermissions.Populate(this, applicationOption.FolderPermissions);

        FirewallRules = new FirewallRulesViewModel();
        FirewallRules.Populate(this, applicationOption.FirewallRules);

        ConfigurationSettingsFiles = new ConfigurationSettingsFilesViewModel();
        ConfigurationSettingsFiles.Populate(this, applicationOption.ConfigurationSettingsFiles);

        SetFilterTextForMenu(string.Empty);

        ComponentType = applicationOption.ComponentType;
        HostingFramework = applicationOption.HostingFramework;
        IsService = applicationOption.ComponentType
            is ComponentType.PostgreSqlServer
            or ComponentType.InternetInformationService
            or ComponentType.WindowsService;
        if (!string.IsNullOrEmpty(applicationOption.RawInstallationPath))
        {
            RawInstallationPath = ResolveTemplateIfNeededByApplicationSettingsLookup(applicationOption.RawInstallationPath);
        }

        InstallationFile = applicationOption.InstallationFile;
        ResolveInstallationPathAndSetInstallationFolderPath(applicationOption);
        ResolveInstalledMainFile(applicationOption);

        foreach (var dependentServiceName in applicationOption.DependentServices)
        {
            DependentServices.Add(new DependentServiceViewModel(dependentServiceName));
        }

        Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);
        Messenger.Default.Register<UpdateDependentServiceStateMessage>(this, HandleDependentServiceState);
    }

    public ILogger<ComponentProviderViewModel> Logger { get; }

    public bool CompactMode
    {
        get => compactMode;
        set
        {
            compactMode = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<ComponentProviderViewModel> RefComponentProviders { get; }

    public DirectoryInfo InstallerTempDirectory { get; }

    public DirectoryInfo InstallationDirectory { get; }

    public bool DisableInstallationActions { get; }

    public string ProjectName { get; }

    public bool ShowOnlyBaseSettings
    {
        get => showOnlyBaseSettings;
        set
        {
            if (showOnlyBaseSettings == value)
            {
                return;
            }

            showOnlyBaseSettings = value;
            RaisePropertyChanged();
        }
    }

    public ApplicationSettingsViewModel DefaultApplicationSettings { get; }

    public ApplicationSettingsViewModel ApplicationSettings { get; }

    public FolderPermissionsViewModel FolderPermissions { get; }

    public FirewallRulesViewModel FirewallRules { get; }

    public ConfigurationSettingsFilesViewModel ConfigurationSettingsFiles { get; }

    public string FilterTextForMenu
    {
        get => filterTextForMenu;
        set
        {
            filterTextForMenu = value;
            RaisePropertyChanged();

            HideMenuItem = !Name.Contains(filterTextForMenu, StringComparison.OrdinalIgnoreCase);

            HighlightedMenuName.HighlightText(
                Name,
                FilterTextForMenu,
                Brushes.Goldenrod,
                useBoldOnHighlightText: true);
        }
    }

    public ObservableCollection<Run> HighlightedMenuName { get; } = new();

    public bool HideMenuItem
    {
        get => hideMenuItem;
        set
        {
            hideMenuItem = value;
            RaisePropertyChanged();
        }
    }

    public string Name { get; }

    public virtual string Description => $"{ComponentType.GetDescription()} / {HostingFramework.GetDescription()}";

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; }

    public HostingFrameworkType HostingFramework { get; }

    public bool IsService { get; }

    public string? RawInstallationPath
    {
        get => rawInstallationPath;
        protected set
        {
            rawInstallationPath = value;
            RaisePropertyChanged();
        }
    }

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

    public ValueTemplateItemViewModel? InstallationFolderPath
    {
        get => installationFolderPath;
        protected set
        {
            installationFolderPath = value;
            RaisePropertyChanged();
        }
    }

    public ValueTemplateItemViewModel? InstalledMainFilePath
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

    public ObservableCollectionEx<RunningStateIssue> RunningStateIssues { get; } = new();

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

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => this.ShowOnlyBaseSettings = obj.ShowOnlyBaseSettings;
}