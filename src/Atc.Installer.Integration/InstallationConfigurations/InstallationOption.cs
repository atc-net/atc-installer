namespace Atc.Installer.Integration.InstallationConfigurations;

public class InstallationOption
{
    public string Name { get; set; } = string.Empty;

    public AzureOptions Azure { get; init; } = new();

    public IList<ApplicationOption> Applications { get; init; } = new List<ApplicationOption>();
}