namespace Atc.Installer.Wpf.ComponentProvider;

public class DependentServiceViewModel : ViewModelBase
{
    private ComponentInstallationState installationState;
    private ComponentRunningState runningState;

    public DependentServiceViewModel(
        string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; private init; }

    public ComponentInstallationState InstallationState
    {
        get => installationState;
        set
        {
            installationState = value;
            RaisePropertyChanged();
        }
    }

    public ComponentRunningState RunningState
    {
        get => runningState;
        set
        {
            runningState = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(InstallationState)}: {InstallationState}, {nameof(RunningState)}: {RunningState}";
}