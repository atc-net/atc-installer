namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
{
    private readonly INetworkShellService networkShellService;
    private readonly IInternetInformationServerInstallerService iisInstallerService;
    private readonly IPostgreSqlServerInstallerService pgSqlInstallerService;
    private readonly IWindowsApplicationInstallerService waInstallerService;
    private readonly ToastNotificationManager notificationManager = new();
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

    private async Task LoadConfigurationFile(
        string file)
    {
        try
        {
            StopMonitoringServices();

            var json = await File
                .ReadAllTextAsync(file)
                .ConfigureAwait(true);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create()) ?? throw new IOException($"Invalid format in {file}");

            Populate(installationOptions);

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