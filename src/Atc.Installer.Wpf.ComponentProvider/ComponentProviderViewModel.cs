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
            InstallationPath = $"C:\\ProgramFiles\\MyApp";
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
        InstallationPath = applicationOption.InstallationPath;
    }

    public string Name { get; private set; }

    public string InstallationPath { get; private set; }

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
            installationState = value;
            RaisePropertyChanged();
        }
    }

    public void StartChecking()
    {
        IsBusy = true;
        InstallationState = ComponentInstallationState.Checking;

        Task.Run(async  () =>
        {
            await Task.Delay(5_000);

            // TODO:...

            InstallationState = ComponentInstallationState.NoInstallationsFiles;
            IsBusy = false;
        });
    }
}