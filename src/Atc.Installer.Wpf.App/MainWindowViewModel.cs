// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
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

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
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
                new Dictionary<string, object>(StringComparer.Ordinal),
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
                new Dictionary<string, object>(StringComparer.Ordinal),
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

    public ApplicationOptionsViewModel ApplicationOptions { get; init; }

    public AzureOptionsViewModel? AzureOptions { get; set; }

    public IDictionary<string, object> DefaultApplicationSettings { get; private set; } = new Dictionary<string, object>(StringComparer.Ordinal);

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

    private void LoadRecentOpenFiles()
    {
        var recentOpenFilesFile = Path.Combine(App.InstallerProgramDataDirectory.FullName, Constants.RecentOpenFilesFileName);
        if (!File.Exists(recentOpenFilesFile))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(recentOpenFilesFile);

            var recentOpenFilesOption = JsonSerializer.Deserialize<RecentOpenFilesOption>(
                json,
                JsonSerializerOptionsFactory.Create()) ?? throw new IOException($"Invalid format in {recentOpenFilesFile}");

            RecentOpenFiles.Clear();

            RecentOpenFiles.SuppressOnChangedNotification = true;
            foreach (var recentOpenFile in recentOpenFilesOption.RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
            {
                if (!File.Exists(recentOpenFile.FilePath))
                {
                    continue;
                }

                RecentOpenFiles.Add(new RecentOpenFileViewModel(App.InstallerProgramDataProjectsDirectory, recentOpenFile.TimeStamp, recentOpenFile.FilePath));
            }

            RecentOpenFiles.SuppressOnChangedNotification = false;
        }
        catch
        {
            // Skip
        }
    }

    private void AddLoadedFileToRecentOpenFiles(
        FileInfo file)
    {
        RecentOpenFiles.Add(new RecentOpenFileViewModel(App.InstallerProgramDataProjectsDirectory, DateTime.Now, file.FullName));

        var recentOpenFilesOption = new RecentOpenFilesOption();
        foreach (var vm in RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
        {
            var item = new RecentOpenFileOption
            {
                TimeStamp = vm.TimeStamp,
                FilePath = vm.File,
            };

            if (recentOpenFilesOption.RecentOpenFiles.FirstOrDefault(x => x.FilePath == item.FilePath) is not null)
            {
                continue;
            }

            if (!File.Exists(item.FilePath))
            {
                continue;
            }

            recentOpenFilesOption.RecentOpenFiles.Add(item);
        }

        var recentOpenFilesFilePath = Path.Combine(App.InstallerProgramDataDirectory.FullName, Constants.RecentOpenFilesFileName);
        if (!Directory.Exists(App.InstallerProgramDataDirectory.FullName))
        {
            Directory.CreateDirectory(App.InstallerProgramDataDirectory.FullName);
        }

        var json = JsonSerializer.Serialize(
            recentOpenFilesOption,
            JsonSerializerOptionsFactory.Create());
        File.WriteAllText(recentOpenFilesFilePath, json);

        LoadRecentOpenFiles();
    }

    private async Task LoadConfigurationFile(
        FileInfo file,
        CancellationToken cancellationToken)
    {
        try
        {
            loggerComponentProvider.Log(LogLevel.Trace, $"Loading configuration file: {file.FullName}");

            StopMonitoringServices();

            var json = await File
                .ReadAllTextAsync(file.FullName, cancellationToken)
                .ConfigureAwait(true);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                JsonSerializerOptionsFactory.Create()) ?? throw new IOException($"Invalid format in {file}");

            installationDirectory = new DirectoryInfo(file.Directory!.FullName);

            ValidateConfigurationFile(installationOptions);

            Populate(installationOptions);

            AddLoadedFileToRecentOpenFiles(file);

            StartMonitoringServices();

            loggerComponentProvider.Log(LogLevel.Trace, $"Loaded configuration file: {file.FullName}");
        }
        catch (Exception ex)
        {
            loggerComponentProvider.Log(LogLevel.Error, $"Configuration file: {file.FullName}, Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private static void ValidateConfigurationFile(
        InstallationOption installationOptions)
    {
        var errors = new List<string>();
        foreach (var applicationOption in installationOptions.Applications)
        {
            if (string.IsNullOrEmpty(applicationOption.Name))
            {
                errors.Add($"{nameof(applicationOption.Name)} is missing");
            }

            if (string.IsNullOrEmpty(applicationOption.RawInstallationPath) &&
                string.IsNullOrEmpty(applicationOption.InstallationFile) &&
                applicationOption.HostingFramework != HostingFrameworkType.NativeNoSettings)
            {
                errors.Add($"{applicationOption.Name}->{nameof(applicationOption.InstallationFile)} is invalid");
            }

            if (string.IsNullOrEmpty(applicationOption.InstallationPath))
            {
                errors.Add($"{applicationOption.Name}->{nameof(applicationOption.InstallationPath)} is invalid");
            }
        }

        if (errors.Any())
        {
            throw new ValidationException(string.Join(Environment.NewLine, errors));
        }
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
        InstallationOption installationOptions)
    {
        ProjectName = installationOptions.Name;
        AzureOptions = new AzureOptionsViewModel(installationOptions.Azure);
        DefaultApplicationSettings = installationOptions.DefaultApplicationSettings;

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
    }
}