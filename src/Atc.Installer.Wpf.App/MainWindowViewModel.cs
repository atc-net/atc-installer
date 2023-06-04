namespace Atc.Installer.Wpf.App;

public partial class MainWindowViewModel : MainWindowViewModelBase
{
    private string? projectName;
    private ComponentProviderViewModel? selectedComponentProvider;

    public MainWindowViewModel()
    {
        if (IsInDesignMode)
        {
            ComponentProviders.Add(
                new WindowsApplicationComponentProviderViewModel(
                    new ApplicationOption
                    {
                        Name = "My-NT-Service",
                        ComponentType = ComponentType.InternetInformationService,
                    }));

            ComponentProviders.Add(
                new InternetInformationServerComponentProviderViewModel(
                    new ApplicationOption
                    {
                        Name = "My-WebApi",
                        ComponentType = ComponentType.InternetInformationService,
                    }));
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
}