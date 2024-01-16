namespace Atc.Installer.Wpf.ComponentProvider.Messages;

public class UpdateDependentServiceStateMessage(
    string name,
    ComponentInstallationState installationState,
    ComponentRunningState runningState)
    : MessageBase
{
    public string Name { get; } = name;

    public ComponentInstallationState InstallationState { get; } = installationState;

    public ComponentRunningState RunningState { get; } = runningState;

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(InstallationState)}: {InstallationState}";
}