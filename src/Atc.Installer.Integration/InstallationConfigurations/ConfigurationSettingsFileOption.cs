// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Integration.InstallationConfigurations;

public class ConfigurationSettingsFileOption
{
    public string FileName { get; set; } = string.Empty;

    public IDictionary<string, object> JsonSettings { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public IList<XmlElementSettingsOptions> XmlSettings { get; init; } = new List<XmlElementSettingsOptions>();

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(JsonSettings)}.Count: {JsonSettings?.Count}, {nameof(XmlSettings)}.Count: {XmlSettings?.Count}";
}