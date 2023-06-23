namespace Atc.Installer.Integration.InstallationConfigurations;

public class ApplicationOption
{
    public string Name { get; set; } = string.Empty;

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; set; }

    public HostingFrameworkType HostingFramework { get; set; }

    public string? InstallationFile { get; set; }

    public string InstallationPath { get; set; } = string.Empty;

    public IList<string> DependentComponents { get; init; } = new List<string>();

    public IList<string> DependentServices { get; init; } = new List<string>();

    public IDictionary<string, object> ApplicationSettings { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IList<FolderPermissionOption> FolderPermissions { get; init; } = new List<FolderPermissionOption>();

    public IList<ConfigurationSettingsFileOption> ConfigurationSettingsFiles { get; init; } = new List<ConfigurationSettingsFileOption>();

    public IList<EndpointOption> Endpoints { get; init; } = new List<EndpointOption>();

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(ComponentType)}: {ComponentType}, {nameof(HostingFramework)}: {HostingFramework}";
}