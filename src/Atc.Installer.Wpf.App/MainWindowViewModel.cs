namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
{
    private string? projectName;
    private ComponentProviderViewModel? selectedComponentProvider;
    private CancellationTokenSource? cancellationTokenSource;

    public MainWindowViewModel()
    {
        if (IsInDesignMode)
        {
            ProjectName = "MyProject";
            ComponentProviders.Add(
                new WindowsApplicationComponentProviderViewModel(
                    ProjectName,
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new ApplicationOption
                    {
                        Name = "My-NT-Service",
                        ComponentType = ComponentType.InternetInformationService,
                    }));

            ComponentProviders.Add(
                new InternetInformationServerComponentProviderViewModel(
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
        IOptions<ApplicationOptions> applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);

        ApplicationOptions = applicationOptions.Value;
        AzureOptions = new AzureOptions();
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

    private void Populate(
        InstallationOption installationOptions)
    {
        ProjectName = installationOptions.Name;
        AzureOptions = installationOptions.Azure;
        DefaultApplicationSettings = installationOptions.DefaultApplicationSettings;

        ComponentProviders.Clear();

        foreach (var appInstallationOption in installationOptions.Applications)
        {
            switch (appInstallationOption.ComponentType)
            {
                case ComponentType.Application or ComponentType.WindowsService:
                {
                    var vm = new WindowsApplicationComponentProviderViewModel(
                        ProjectName,
                        DefaultApplicationSettings,
                        appInstallationOption);
                    ComponentProviders.Add(vm);
                    break;
                }

                case ComponentType.InternetInformationService:
                {
                    var vm = new InternetInformationServerComponentProviderViewModel(
                        ProjectName,
                        DefaultApplicationSettings,
                        appInstallationOption);
                    ComponentProviders.Add(vm);
                    break;
                }
            }

            if (ComponentProviders.Count == 1)
            {
                SelectedComponentProvider = ComponentProviders[0];
            }
        }

        foreach (var vm in ComponentProviders)
        {
            vm.PrepareInstallationFiles(unpackIfIfExist: false);
            vm.AnalyzeAndUpdateStatesInBackgroundThread();
        }
    }
}