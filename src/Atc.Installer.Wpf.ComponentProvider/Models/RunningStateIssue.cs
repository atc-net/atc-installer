namespace Atc.Installer.Wpf.ComponentProvider.Models;

public class RunningStateIssue
{
    public string Name { get; set; } = string.Empty;

    public ComponentInstallationState InstallationState { get; set; }

    public ComponentRunningState RunningState { get; set; }

    public string ToDisplay()
        => $"Name: {Name}, Installation state: {InstallationState}, Running state: {RunningState}";

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(InstallationState)}: {InstallationState}, {nameof(RunningState)}: {RunningState}";
}