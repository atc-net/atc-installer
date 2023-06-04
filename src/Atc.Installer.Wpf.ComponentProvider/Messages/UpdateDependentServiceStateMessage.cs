namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class UpdateDependentServiceStateMessage : MessageBase
{
    public UpdateDependentServiceStateMessage(
        string name,
        ComponentInstallationState installationState,
        ComponentRunningState runningState)
    {
        Name = name;
        InstallationState = installationState;
        RunningState = runningState;
    }

    public string Name { get; set; }

    public ComponentInstallationState InstallationState { get; set; }

    public ComponentRunningState RunningState { get; set; }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(InstallationState)}: {InstallationState}";
}