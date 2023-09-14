// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public partial class MainWindowViewModel : MainWindowViewModelBase, IMainWindowViewModel
{
    private readonly ILogger<ComponentProviderViewModel> loggerComponentProvider;
    private readonly IGitHubReleaseService gitHubReleaseService;
    private readonly INetworkShellService networkShellService;
    private readonly IWindowsFirewallService windowsFirewallService;
    private readonly IElasticSearchServerInstallerService esInstallerService;
    private readonly IInternetInformationServerInstallerService iisInstallerService;
    private readonly IPostgreSqlServerInstallerService pgSqlInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private readonly IAzureStorageAccountInstallerService azureStorageAccountInstallerService;
    private readonly ICheckForUpdatesBoxDialogViewModel checkForUpdatesBoxDialogViewModel;
    private readonly ToastNotificationManager notificationManager = new();
    private string? newVersionIsAvailable;
    private DirectoryInfo? installationDirectory;
    private string? projectName;
    private ComponentProviderViewModel? selectedComponentProvider;
    private CancellationTokenSource? cancellationTokenSource;

    public MainWindowViewModel()
    {
        this.loggerComponentProvider = NullLogger<ComponentProviderViewModel>.Instance;

        this.gitHubReleaseService = new GitHubReleaseService();

        var installedAppsInstallerService = new InstalledAppsInstallerService();
        this.networkShellService = new NetworkShellService();
        this.windowsFirewallService = new WindowsFirewallService();

        this.waInstallerService = new WindowsApplicationInstallerService(installedAppsInstallerService);
        this.iisInstallerService = new InternetInformationServerInstallerService(installedAppsInstallerService);

        this.esInstallerService = new ElasticSearchServerInstallerService(waInstallerService, installedAppsInstallerService);
        this.pgSqlInstallerService = new PostgreSqlServerInstallerService(waInstallerService, installedAppsInstallerService);

        this.azureStorageAccountInstallerService = new AzureStorageAccountInstallerService();

        this.checkForUpdatesBoxDialogViewModel = new CheckForUpdatesBoxDialogViewModel(gitHubReleaseService);

        this.installationDirectory = new DirectoryInfo(Path.Combine(App.InstallerTempDirectory.FullName, "InstallationFiles"));

        ApplicationOptions = new ApplicationOptionsViewModel(new ApplicationOptions());

        if (!IsInDesignMode)
        {
            return;
        }

        ProjectName = "MyProject";
        ComponentProviders.Add(
            new WindowsApplicationComponentProviderViewModel(
                NullLogger<ComponentProviderViewModel>.Instance,
                waInstallerService,
                networkShellService,
                windowsFirewallService,
                new ObservableCollectionEx<ComponentProviderViewModel>(),
                App.InstallerTempDirectory,
                installationDirectory,
                ProjectName,
                new ObservableCollectionEx<KeyValueTemplateItemViewModel>(),
                new ApplicationOption
                {
                    Name = "My-NT-Service",
                    ComponentType = ComponentType.InternetInformationService,
                }));

        ComponentProviders.Add(
            new InternetInformationServerComponentProviderViewModel(
                NullLogger<ComponentProviderViewModel>.Instance,
                iisInstallerService,
                networkShellService,
                windowsFirewallService,
                ComponentProviders,
                App.InstallerTempDirectory,
                installationDirectory,
                ProjectName,
                new ObservableCollectionEx<KeyValueTemplateItemViewModel>(),
                new ApplicationOption
                {
                    Name = "My-WebApi",
                    ComponentType = ComponentType.InternetInformationService,
                }));
    }

    public MainWindowViewModel(
        ILogger<ComponentProviderViewModel> loggerComponentProvider,
        IGitHubReleaseService gitHubReleaseService,
        INetworkShellService networkShellService,
        IWindowsFirewallService windowsFirewallService,
        IElasticSearchServerInstallerService elasticSearchServerInstallerService,
        IInternetInformationServerInstallerService internetInformationServerInstallerService,
        IPostgreSqlServerInstallerService postgreSqlServerInstallerService,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        IAzureStorageAccountInstallerService azureStorageAccountInstallerService,
        ICheckForUpdatesBoxDialogViewModel checkForUpdatesBoxDialogViewModel,
        IOptions<ApplicationOptions> applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);
        var applicationOptionsValue = applicationOptions.Value;

        this.loggerComponentProvider = loggerComponentProvider ?? throw new ArgumentNullException(nameof(loggerComponentProvider));
        this.gitHubReleaseService = gitHubReleaseService ?? throw new ArgumentNullException(nameof(gitHubReleaseService));
        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));
        this.windowsFirewallService = windowsFirewallService ?? throw new ArgumentNullException(nameof(windowsFirewallService));
        this.esInstallerService = elasticSearchServerInstallerService ?? throw new ArgumentNullException(nameof(elasticSearchServerInstallerService));
        this.iisInstallerService = internetInformationServerInstallerService ?? throw new ArgumentNullException(nameof(internetInformationServerInstallerService));
        this.pgSqlInstallerService = postgreSqlServerInstallerService ?? throw new ArgumentNullException(nameof(postgreSqlServerInstallerService));
        this.waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        this.azureStorageAccountInstallerService = azureStorageAccountInstallerService ?? throw new ArgumentNullException(nameof(azureStorageAccountInstallerService));
        this.checkForUpdatesBoxDialogViewModel = checkForUpdatesBoxDialogViewModel ?? throw new ArgumentNullException(nameof(checkForUpdatesBoxDialogViewModel));

        loggerComponentProvider.Log(LogLevel.Trace, $"Starting {AssemblyHelper.GetSystemName()} - Version: {AssemblyHelper.GetSystemVersion()}");

        LoadRecentOpenFiles();

        ApplicationOptions = new ApplicationOptionsViewModel(applicationOptionsValue);
        AzureOptions = new AzureOptionsViewModel();

        Messenger.Default.Register<ToastNotificationMessage>(this, HandleToastNotificationMessage);
        Messenger.Default.Register<RefreshSelectedComponentProviderMessage>(this, HandleRefreshSelectedComponentProviderMessage);

        loggerComponentProvider.Log(LogLevel.Trace, $"{AssemblyHelper.GetSystemName()} is started");

        if (ApplicationOptions.OpenRecentConfigurationFileOnStartup &&
            RecentOpenFiles.Count > 0)
        {
            OpenRecentConfigurationFileCommand.ExecuteAsync(RecentOpenFiles[0].File);
        }

        Task.Factory.StartNew(
            async () => await CheckForUpdates().ConfigureAwait(false),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public string? NewVersionIsAvailable
    {
        get => newVersionIsAvailable;
        set
        {
            newVersionIsAvailable = value;
            OnPropertyChanged();
        }
    }

    public ApplicationOptionsViewModel ApplicationOptions { get; set; }

    public AzureOptionsViewModel? AzureOptions { get; set; }

    public ObservableCollectionEx<KeyValueTemplateItemViewModel> DefaultApplicationSettings { get; private set; } = new();

    public FileInfo? InstallationFile { get; private set; }

    public string? ProjectName
    {
        get => projectName;
        set
        {
            projectName = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<RecentOpenFileViewModel> RecentOpenFiles { get; } = new();

    public ObservableCollectionEx<ComponentProviderViewModel> ComponentProviders { get; } = new();

    public ComponentProviderViewModel? SelectedComponentProvider
    {
        get => selectedComponentProvider;
        set
        {
            selectedComponentProvider = value;
            RaisePropertyChanged();
        }
    }

    public new void OnKeyDown(
        object sender,
        KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnKeyDown(sender, e);

        if (!e.Handled &&
            Keyboard.Modifiers == ModifierKeys.Control &&
            e.Key == Key.O)
        {
            OpenConfigurationFileCommandHandler().ConfigureAwait(continueOnCapturedContext: false);
        }

        if (!e.Handled &&
            Keyboard.Modifiers == ModifierKeys.Control &&
            e.Key == Key.R &&
            CanOpenRecentConfigurationFileCommandHandler())
        {
            OpenRecentConfigurationFileCommandHandler(RecentOpenFiles[0].File).ConfigureAwait(continueOnCapturedContext: false);
        }

        if (!e.Handled &&
            Keyboard.Modifiers == ModifierKeys.Control &&
            e.Key == Key.S &&
            CanSaveConfigurationFileCommandHandler())
        {
            SaveConfigurationFileCommandHandler().ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    private void HandleToastNotificationMessage(
        ToastNotificationMessage obj)
    {
        notificationManager.Show(
            useDesktop: false,
            new ToastNotificationContent(
                obj.ToastNotificationType,
                obj.Title,
                obj.Message),
            areaName: "ToastNotificationArea");
    }

    private void HandleRefreshSelectedComponentProviderMessage(
        RefreshSelectedComponentProviderMessage obj)
        => RaisePropertyChanged(nameof(SelectedComponentProvider));

    private async Task CheckForUpdates()
    {
        if (!NetworkInformationHelper.HasConnection())
        {
            return;
        }

        var currentVersion = Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version!
                .ToString();

        var latestVersion = await gitHubReleaseService
            .GetLatestVersion()
            .ConfigureAwait(false);

        if (latestVersion is null)
        {
            return;
        }

        if (Version.TryParse(currentVersion, out var cv) &&
            Version.TryParse(latestVersion.ToString(), out var lv) &&
            lv.GreaterThan(cv))
        {
            NewVersionIsAvailable = "New version of the installer is available";
        }
    }

    private void StartMonitoringServices()
    {
        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(
            async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task
                        .Delay(TimeSpan.FromSeconds(3), CancellationToken.None)
                        .ConfigureAwait(true);

                    foreach (var vm in ComponentProviders)
                    {
                        if (!vm.IsBusy)
                        {
                            vm.CheckServiceState();
                        }
                    }
                }
            },
            cancellationTokenSource.Token);
    }

    public void StopMonitoringServices()
    {
        cancellationTokenSource?.Cancel();
    }

    private void AddComponentProviderWindowsApplication(
        ApplicationOption appInstallationOption)
    {
        if (installationDirectory is null)
        {
            return;
        }

        var vm = new WindowsApplicationComponentProviderViewModel(
            loggerComponentProvider,
            waInstallerService,
            networkShellService,
            windowsFirewallService,
            ComponentProviders,
            App.InstallerTempDirectory,
            installationDirectory,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void AddComponentProviderElasticSearchServer(
        ApplicationOption appInstallationOption)
    {
        if (installationDirectory is null)
        {
            return;
        }

        var vm = new ElasticSearchServerComponentProviderViewModel(
            loggerComponentProvider,
            esInstallerService,
            networkShellService,
            windowsFirewallService,
            waInstallerService,
            ComponentProviders,
            App.InstallerTempDirectory,
            installationDirectory,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void AddComponentProviderInternetInformationServer(
        ApplicationOption appInstallationOption)
    {
        if (installationDirectory is null)
        {
            return;
        }

        var vm = new InternetInformationServerComponentProviderViewModel(
            loggerComponentProvider,
            iisInstallerService,
            networkShellService,
            windowsFirewallService,
            ComponentProviders,
            App.InstallerTempDirectory,
            installationDirectory,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void AddComponentProviderPostgreSql(
        ApplicationOption appInstallationOption)
    {
        if (installationDirectory is null)
        {
            return;
        }

        var vm = new PostgreSqlServerComponentProviderViewModel(
            loggerComponentProvider,
            pgSqlInstallerService,
            networkShellService,
            windowsFirewallService,
            waInstallerService,
            ComponentProviders,
            App.InstallerTempDirectory,
            installationDirectory,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void Populate(
        FileInfo installationFile,
        InstallationOption installationOptions)
    {
        InstallationFile = installationFile;
        ProjectName = installationOptions.Name;
        AzureOptions = new AzureOptionsViewModel(installationOptions.Azure);
        DefaultApplicationSettings = new ObservableCollectionEx<KeyValueTemplateItemViewModel>();
        foreach (var item in installationOptions.DefaultApplicationSettings)
        {
            DefaultApplicationSettings.Add(
                new KeyValueTemplateItemViewModel(
                    item.Key,
                    item.Value,
                    template: null,
                    templateLocations: new List<string> { nameof(DefaultApplicationSettings) }));
        }

        ComponentProviders.Clear();

        ComponentProviders.SuppressOnChangedNotification = true;
        foreach (var appInstallationOption in installationOptions.Applications)
        {
            switch (appInstallationOption.ComponentType)
            {
                case ComponentType.Application or ComponentType.WindowsService:
                {
                    AddComponentProviderWindowsApplication(appInstallationOption);
                    break;
                }

                case ComponentType.ElasticSearchServer:
                {
                    AddComponentProviderElasticSearchServer(appInstallationOption);
                    break;
                }

                case ComponentType.InternetInformationService:
                {
                    AddComponentProviderInternetInformationServer(appInstallationOption);
                    break;
                }

                case ComponentType.PostgreSqlServer:
                {
                    AddComponentProviderPostgreSql(appInstallationOption);
                    break;
                }
            }
        }

        foreach (var componentProvider in ComponentProviders)
        {
            componentProvider.ConfigurationSettingsFiles.ResolveValueAndTemplateReferences();
        }

        ComponentProviders.SuppressOnChangedNotification = false;

        if (ComponentProviders.Count > 0)
        {
            SelectedComponentProvider = ComponentProviders[0];
        }

        foreach (var vm in ComponentProviders)
        {
            vm.PrepareInstallationFiles(unpackIfExist: false);
            vm.AnalyzeAndUpdateStatesInBackgroundThread();
        }

        RaisePropertyChanged(nameof(ComponentProviders));

        Messenger.Default.Send(
            new UpdateApplicationOptionsMessage(
                ApplicationOptions.EnableEditingMode,
                ApplicationOptions.ShowOnlyBaseSettings));
    }
}