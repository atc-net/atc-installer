namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
{
    private readonly INetworkShellService networkShellService;
    private readonly IInternetInformationServerInstallerService iisInstallerService;
    private readonly IPostgreSqlServerInstallerService pgSqlInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private readonly ToastNotificationManager notificationManager = new();
    private readonly string installerTempFolder = Path.Combine(Path.GetTempPath(), "atc-installer");
    private string? projectName;
    private ComponentProviderViewModel? selectedComponentProvider;
    private CancellationTokenSource? cancellationTokenSource;

    public MainWindowViewModel()
    {
        var installedAppsInstallerService = new InstalledAppsInstallerService();
        this.networkShellService = new NetworkShellService();

        this.iisInstallerService = new InternetInformationServerInstallerService(installedAppsInstallerService);
        this.waInstallerService = new WindowsApplicationInstallerService(installedAppsInstallerService);
        this.pgSqlInstallerService = new PostgreSqlServerInstallerService(waInstallerService, installedAppsInstallerService);

        if (IsInDesignMode)
        {
            ProjectName = "MyProject";
            ComponentProviders.Add(
                new WindowsApplicationComponentProviderViewModel(
                    waInstallerService,
                    networkShellService,
                    installerTempFolder,
                    ProjectName,
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new ApplicationOption
                    {
                        Name = "My-NT-Service",
                        ComponentType = ComponentType.InternetInformationService,
                    }));

            ComponentProviders.Add(
                new InternetInformationServerComponentProviderViewModel(
                    iisInstallerService,
                    networkShellService,
                    installerTempFolder,
                    ProjectName,
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new ApplicationOption
                    {
                        Name = "My-WebApi",
                        ComponentType = ComponentType.InternetInformationService,
                    }));
        }
    }

    public MainWindowViewModel(
        INetworkShellService networkShellService,
        IInternetInformationServerInstallerService internetInformationServerInstallerService,
        IPostgreSqlServerInstallerService postgreSqlServerInstallerService,
        IWindowsApplicationInstallerService windowsApplicationInstallerService,
        IOptions<ApplicationOptions> applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);

        this.networkShellService = networkShellService ?? throw new ArgumentNullException(nameof(networkShellService));
        this.iisInstallerService = internetInformationServerInstallerService ?? throw new ArgumentNullException(nameof(internetInformationServerInstallerService));
        this.pgSqlInstallerService = postgreSqlServerInstallerService ?? throw new ArgumentNullException(nameof(postgreSqlServerInstallerService));
        this.waInstallerService = windowsApplicationInstallerService ?? throw new ArgumentNullException(nameof(windowsApplicationInstallerService));

        LoadRecentOpenFiles();

        ApplicationOptions = applicationOptions.Value;
        AzureOptions = new AzureOptions();

        Messenger.Default.Register<ToastNotificationMessage>(this, HandleToastNotificationMessage);
    }

    public ApplicationOptions? ApplicationOptions { get; init; }

    public AzureOptions? AzureOptions { get; set; }

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

    private void StartMonitoringServices()
    {
        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(
            async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task
                        .Delay(3_000, CancellationToken.None)
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
        var recentOpenFilesFile = Path.Combine(installerTempFolder, "RecentOpenFiles.json");
        if (!File.Exists(recentOpenFilesFile))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(recentOpenFilesFile);

            var recentOpenFilesOption = JsonSerializer.Deserialize<RecentOpenFilesOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create()) ?? throw new IOException($"Invalid format in {recentOpenFilesFile}");

            RecentOpenFiles.Clear();

            RecentOpenFiles.SuppressOnChangedNotification = true;
            foreach (var recentOpenFile in recentOpenFilesOption.RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
            {
                if (!File.Exists(recentOpenFile.File))
                {
                    continue;
                }

                RecentOpenFiles.Add(new RecentOpenFileViewModel(recentOpenFile.TimeStamp, recentOpenFile.File));
            }

            RecentOpenFiles.SuppressOnChangedNotification = false;
        }
        catch
        {
            // Skip
        }
    }

    private void AddLoadedFileToRecentOpenFiles(
        string file)
    {
        RecentOpenFiles.Add(new RecentOpenFileViewModel(DateTime.Now, file));

        var recentOpenFilesOption = new RecentOpenFilesOption();
        foreach (var vm in RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
        {
            var item = new RecentOpenFileOption
            {
                TimeStamp = vm.TimeStamp,
                File = vm.File,
            };

            if (recentOpenFilesOption.RecentOpenFiles.FirstOrDefault(x => x.File == item.File) is not null)
            {
                continue;
            }

            if (!File.Exists(item.File))
            {
                continue;
            }

            recentOpenFilesOption.RecentOpenFiles.Add(item);
        }

        var recentOpenFilesFile = Path.Combine(installerTempFolder, "RecentOpenFiles.json");
        if (!Directory.Exists(installerTempFolder))
        {
            Directory.CreateDirectory(installerTempFolder);
        }

        var json = JsonSerializer.Serialize(
            recentOpenFilesOption,
            Serialization.JsonSerializerOptionsFactory.Create());
        File.WriteAllText(recentOpenFilesFile, json);

        LoadRecentOpenFiles();
    }

    private async Task LoadConfigurationFile(
        string file,
        CancellationToken cancellationToken)
    {
        try
        {
            StopMonitoringServices();

            var json = await File
                .ReadAllTextAsync(file, cancellationToken)
                .ConfigureAwait(true);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create()) ?? throw new IOException($"Invalid format in {file}");

            Populate(installationOptions);

            AddLoadedFileToRecentOpenFiles(file);

            StartMonitoringServices();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private void AddComponentProviderWindowsApplication(
        ApplicationOption appInstallationOption)
    {
        var vm = new WindowsApplicationComponentProviderViewModel(
            waInstallerService,
            networkShellService,
            installerTempFolder,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void AddComponentProviderInternetInformationServer(
        ApplicationOption appInstallationOption)
    {
        var vm = new InternetInformationServerComponentProviderViewModel(
            iisInstallerService,
            networkShellService,
            installerTempFolder,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void AddComponentProviderPostgreSql(
        ApplicationOption appInstallationOption)
    {
        var vm = new PostgreSqlServerComponentProviderViewModel(
            pgSqlInstallerService,
            waInstallerService,
            installerTempFolder,
            ProjectName!,
            DefaultApplicationSettings,
            appInstallationOption);
        ComponentProviders.Add(vm);
    }

    private void Populate(
        InstallationOption installationOptions)
    {
        ProjectName = installationOptions.Name;
        AzureOptions = installationOptions.Azure;
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
            vm.PrepareInstallationFiles(unpackIfIfExist: false);
            vm.AnalyzeAndUpdateStatesInBackgroundThread();
        }
    }
}