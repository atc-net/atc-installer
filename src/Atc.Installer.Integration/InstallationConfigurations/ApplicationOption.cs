namespace Atc.Installer.Integration.InstallationConfigurations;

public class ApplicationOption
{
    public string Name { get; set; } = string.Empty;

    public ComponentType ComponentType { get; set; }

    public HostingFrameworkType HostingFramework { get; set; }

    public string InstallationPath { get; set; } = string.Empty;

    public IList<string> DependentServices { get; init; } = new List<string>();

    public IDictionary<string, object> ApplicationSettings { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IDictionary<string, object> InternetInformationServiceSettings { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(ComponentType)}: {ComponentType}, {nameof(HostingFramework)}: {HostingFramework}";
}