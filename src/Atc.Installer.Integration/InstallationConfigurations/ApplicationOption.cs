namespace Atc.Installer.Integration.InstallationConfigurations;

public class ApplicationOption
{
    public string Name { get; set; } = string.Empty;

    public string? ServiceName { get; set; }

    public ComponentType ComponentType { get; set; }

    public HostingFrameworkType HostingFramework { get; set; }

    public string? InstallationFile { get; set; }

    public string InstallationPath { get; set; } = string.Empty;

    public string? RawInstallationPath { get; set; }

    public bool DisableInstallationActions { get; set; }

    public IList<string> DependentComponents { get; set; } = new List<string>();

    public IList<string> DependentServices { get; init; } = new List<string>();

    public IDictionary<string, object> ApplicationSettings { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IList<FolderPermissionOption> FolderPermissions { get; set; } = new List<FolderPermissionOption>();

    public IList<RegistrySettingOption> RegistrySettings { get; set; } = new List<RegistrySettingOption>();

    public IList<FirewallRuleOption> FirewallRules { get; set; } = new List<FirewallRuleOption>();

    public IList<ConfigurationSettingsFileOption> ConfigurationSettingsFiles { get; set; } = new List<ConfigurationSettingsFileOption>();

    public IList<EndpointOption> Endpoints { get; init; } = new List<EndpointOption>();

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(ComponentType)}: {ComponentType}, {nameof(HostingFramework)}: {HostingFramework}";
}