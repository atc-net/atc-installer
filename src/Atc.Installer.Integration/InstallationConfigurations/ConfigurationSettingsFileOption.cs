// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Integration.InstallationConfigurations;

public class ConfigurationSettingsFileOption
{
    public string FileName { get; set; } = string.Empty;

    public IDictionary<string, object> Settings { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(Settings)}.Count: {Settings?.Count}";
}