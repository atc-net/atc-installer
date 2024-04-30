// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public partial class MainWindowViewModel : MainWindowViewModelBase, IMainWindowViewModel
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<MainWindowViewModel> logger;
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
    private AzureOptionsViewModel? azureOptions;
    private ApplicationOptionsViewModel applicationOptions = new();
    private DirectoryInfo? installationDirectory;
    private BitmapImage? icon;
    private string? projectName;
    private bool compactMode;
    private string? componentProviderFilter;
    private ComponentProviderViewModel? selectedComponentProvider;
    private CancellationTokenSource? cancellationTokenSource;

    public MainWindowViewModel()
    {
        loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = NullLogger<MainWindowViewModel>.Instance;

        gitHubReleaseService = new GitHubReleaseService();

        var installedAppsInstallerService = new InstalledAppsInstallerService();
        networkShellService = new NetworkShellService();
        windowsFirewallService = new WindowsFirewallService();

        waInstallerService = new WindowsApplicationInstallerService(installedAppsInstallerService);
        iisInstallerService = new InternetInformationServerInstallerService(installedAppsInstallerService);

        esInstallerService = new ElasticSearchServerInstallerService(waInstallerService, installedAppsInstallerService);
        pgSqlInstallerService = new PostgreSqlServerInstallerService(waInstallerService, installedAppsInstallerService);

        azureStorageAccountInstallerService = new AzureStorageAccountInstallerService();

        checkForUpdatesBoxDialogViewModel = new CheckForUpdatesBoxDialogViewModel(gitHubReleaseService);

        installationDirectory = new DirectoryInfo(Path.Combine(App.InstallerTempDirectory.FullName, "InstallationFiles"));

        ApplicationOptions = new ApplicationOptionsViewModel(new ApplicationOptions());

        if (!IsInDesignMode)
        {
            return;
        }

        ProjectName = "MyProject";
        ComponentProviders.Add(
            new WindowsApplicationComponentProviderViewModel(
                NullLoggerFactory.Instance,
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
                NullLoggerFactory.Instance,
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
        ILoggerFactory loggerFactory,
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

        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        logger = loggerFactory.CreateLogger<MainWindowViewModel>();
        this.gitHubReleaseService = gitHubReleaseService ?? throw new ArgumentNullException(nameof(gitHubReleaseService));
        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));
        this.windowsFirewallService = windowsFirewallService ?? throw new ArgumentNullException(nameof(windowsFirewallService));
        esInstallerService = elasticSearchServerInstallerService ?? throw new ArgumentNullException(nameof(elasticSearchServerInstallerService));
        iisInstallerService = internetInformationServerInstallerService ?? throw new ArgumentNullException(nameof(internetInformationServerInstallerService));
        pgSqlInstallerService = postgreSqlServerInstallerService ?? throw new ArgumentNullException(nameof(postgreSqlServerInstallerService));
        waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));
        this.azureStorageAccountInstallerService = azureStorageAccountInstallerService ?? throw new ArgumentNullException(nameof(azureStorageAccountInstallerService));
        this.checkForUpdatesBoxDialogViewModel = checkForUpdatesBoxDialogViewModel ?? throw new ArgumentNullException(nameof(checkForUpdatesBoxDialogViewModel));

        logger.Log(LogLevel.Trace, $"Starting {AssemblyHelper.GetSystemName()} - Version: {AssemblyHelper.GetSystemVersion()}");

        ApplicationOptions = new ApplicationOptionsViewModel(applicationOptionsValue);
        Icon = ApplicationOptions.Icon ?? App.DefaultIcon;
        AzureOptions = new AzureOptionsViewModel();

        LoadRecentOpenFiles();

        Messenger.Default.Register<ToastNotificationMessage>(this, HandleToastNotificationMessage);
        Messenger.Default.Register<RefreshSelectedComponentProviderMessage>(this, HandleRefreshSelectedComponentProviderMessage);
        Messenger.Default.Register<UpdateDefaultApplicationSettingsMessage>(this, HandleUpdateDefaultApplicationSettingsMessage);

        logger.Log(LogLevel.Trace, $"{AssemblyHelper.GetSystemName()} is started");

        if (ApplicationOptions.OpenRecentFileOnStartup &&
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

    public ApplicationOptionsViewModel ApplicationOptions
    {
        get => applicationOptions;
        set
        {
            applicationOptions = value;
            RaisePropertyChanged();
        }
    }

    public AzureOptionsViewModel? AzureOptions
    {
        get => azureOptions;
        set
        {
            azureOptions = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<KeyValueTemplateItemViewModel> DefaultApplicationSettings { get; private set; } = new();

    public FileInfo? InstallationFile { get; private set; }

    public BitmapImage? Icon
    {
        get => icon;
        set
        {
            icon = value;
            RaisePropertyChanged();
        }
    }

    public string? ProjectName
    {
        get => projectName;
        set
        {
            projectName = value;
            RaisePropertyChanged();
        }
    }

    public bool CompactMode
    {
        get => compactMode;
        set
        {
            compactMode = value;
            RaisePropertyChanged();

            foreach (var provider in ComponentProviders)
            {
                provider.CompactMode = compactMode;
            }
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

    public string? ComponentProviderFilter
    {
        get => componentProviderFilter;
        set
        {
            componentProviderFilter = value;
            RaisePropertyChanged();

            foreach (var vm in ComponentProviders)
            {
                vm.SetFilterTextForMenu(componentProviderFilter ?? string.Empty);
            }
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

    private void HandleUpdateDefaultApplicationSettingsMessage(
        UpdateDefaultApplicationSettingsMessage obj)
    {
        switch (obj.TriggerActionType)
        {
            case TriggerActionType.Insert:
                if (DefaultApplicationSettings.FirstOrDefault(x => x.Key == obj.KeyValueTemplateItem.Key) is null)
                {
                    DefaultApplicationSettings.Add(obj.KeyValueTemplateItem);
                }

                break;
            case TriggerActionType.Update:
                var itemToUpdate = DefaultApplicationSettings.FirstOrDefault(x => x.Key == obj.KeyValueTemplateItem.Key);
                if (itemToUpdate is not null)
                {
                    itemToUpdate.Value = obj.KeyValueTemplateItem.Value;
                }

                break;
            case TriggerActionType.Delete:
                var itemToDelete = DefaultApplicationSettings.FirstOrDefault(x => x.Key == obj.KeyValueTemplateItem.Key);
                if (itemToDelete is not null)
                {
                    DefaultApplicationSettings.Remove(itemToDelete);
                }

                break;
            default:
                throw new SwitchExpressionException(obj.TriggerActionType);
        }
    }

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

    [SuppressMessage("Blocker Bug", "S2930:\"IDisposables\" should be disposed", Justification = "OK.")]
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
                        if (vm.IsBusy)
                        {
                            continue;
                        }

                        vm.CheckPrerequisitesState();
                        vm.CheckServiceState();
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
            loggerFactory,
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
            loggerFactory,
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
            loggerFactory,
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
            loggerFactory,
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

    [SuppressMessage("Major Bug", "S2583:Conditionally executed code should be reachable", Justification = "OK.")]
    [SuppressMessage("Minor Bug", "S4158:Empty collections should not be accessed or iterated", Justification = "OK.")]
    private void Populate(
        FileInfo installationFile,
        InstallationOption installationOptions)
    {
        InstallationFile = installationFile;
        ProjectName = installationOptions.Name;
        Icon = string.IsNullOrEmpty(installationOptions.Icon)
            ? ApplicationOptions.Icon ?? App.DefaultIcon
            : Atc.Wpf.Helpers.BitmapImageHelper.ConvertFromBase64(installationOptions.Icon);
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
        ComponentProviderFilter = string.Empty;

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