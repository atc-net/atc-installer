namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
{
    private string? projectName;
    private ComponentProviderViewModel? selectedComponentProvider;

    public MainWindowViewModel()
    {
        if (IsInDesignMode)
        {
            ProjectName = "MyProject";
            ComponentProviders.Add(
                new WindowsApplicationComponentProviderViewModel(
                    ProjectName,
                    new ApplicationOption
                    {
                        Name = "My-NT-Service",
                        ComponentType = ComponentType.InternetInformationService,
                    }));

            ComponentProviders.Add(
                new InternetInformationServerComponentProviderViewModel(
                    ProjectName,
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

    private async Task LoadConfigurationFile(
        string file)
    {
        try
        {
            var json = await File.ReadAllTextAsync(file);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                Serialization.JsonSerializerOptionsFactory.Create());

            if (installationOptions is null)
            {
                throw new IOException($"Invalid format in {file}");
            }

            Populate(installationOptions);
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

        ComponentProviders.Clear();

        foreach (var appInstallationOption in installationOptions.Applications)
        {
            switch (appInstallationOption.ComponentType)
            {
                case ComponentType.Application or ComponentType.WindowsService:
                {
                    var vm = new WindowsApplicationComponentProviderViewModel(
                        ProjectName,
                        appInstallationOption);
                    ComponentProviders.Add(vm);
                    break;
                }

                case ComponentType.InternetInformationService:
                {
                    var vm = new InternetInformationServerComponentProviderViewModel(
                        ProjectName,
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

        // TODO: REMOVE
        //foreach (var vm in ComponentProviders.Where(x => x.Name == "Schur.Connector.KEPServerEX.Api"))
        //foreach (var vm in ComponentProviders.Where(x => x.Name == "Schur.PrintServer.NiceLabel10"))
        foreach (var vm in ComponentProviders)
        {
            vm.PrepareInstallationFiles();
            vm.StartChecking();
        }
    }
}