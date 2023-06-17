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

    public string Name { get; }

    public ComponentInstallationState InstallationState { get; }

    public ComponentRunningState RunningState { get; }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(InstallationState)}: {InstallationState}";
}