namespace Atc.Installer.Integration.InstallationConfigurations;

public class InstallationOption
{
    public string Name { get; set; } = string.Empty;

    public AzureOptions Azure { get; set; } = new();

    public IDictionary<string, object> DefaultApplicationSettings { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IList<ApplicationOption> Applications { get; set; } = new List<ApplicationOption>();
}