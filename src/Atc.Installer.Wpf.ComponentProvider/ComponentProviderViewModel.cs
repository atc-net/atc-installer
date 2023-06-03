namespace Atc.Installer.Wpf.ComponentProvider;

public class ComponentProviderViewModel : ViewModelBase, IComponentProvider
{
    private ComponentInstallationState installationState;

    public ComponentProviderViewModel()
    {
        if (IsInDesignMode)
        {
            InstallationState = ComponentInstallationState.Checking;
            Name = "MyApp";
        }
        else
        {
            throw new DesignTimeUseOnlyException();
        }
    }

    public ComponentProviderViewModel(
        ApplicationOption applicationOption)
    {
        ArgumentNullException.ThrowIfNull(applicationOption);

        Name = applicationOption.Name;
    }

    public string Name { get; set; }

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
            installationState = value;
            RaisePropertyChanged();
        }
    }
}