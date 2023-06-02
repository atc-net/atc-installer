namespace Atc.Installer.Wpf.App.InstallationConfigurations;

public class InstallationOption
{
    public IList<ApplicationOption> Applications { get; init; } = new List<ApplicationOption>();
}