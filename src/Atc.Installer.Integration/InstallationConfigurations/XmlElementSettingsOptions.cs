namespace Atc.Installer.Integration.InstallationConfigurations;

public class XmlElementSettingsOptions
{
    public string Path { get; set; } = string.Empty;

    public string Element { get; set; } = string.Empty;

    public IDictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public override string ToString()
        => $"{nameof(Path)}: {Path}, {nameof(Element)}: {Element}, {nameof(Attributes)}.Count: {Attributes?.Count}";
}