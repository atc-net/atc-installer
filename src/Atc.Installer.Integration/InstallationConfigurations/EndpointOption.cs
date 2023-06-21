namespace Atc.Installer.Integration.InstallationConfigurations;

public class EndpointOption
{
    public string Name { get; set; } = string.Empty;

    public ComponentEndpointType EndpointType { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(EndpointType)}: {EndpointType}, {nameof(Endpoint)}: {Endpoint}";
}