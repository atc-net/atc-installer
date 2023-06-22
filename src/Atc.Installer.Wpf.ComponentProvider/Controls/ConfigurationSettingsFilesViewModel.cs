// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class ConfigurationSettingsFilesViewModel : ViewModelBase
{
    public ObservableCollectionEx<ConfigurationSettingsJsonFileViewModel> JsonItems { get; init; } = new();

    public ObservableCollectionEx<ConfigurationSettingsXmlFileViewModel> XmlItems { get; init; } = new();

    public void Populate(
        IList<ConfigurationSettingsFileOption> configurationSettingsFiles)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFiles);

        JsonItems.Clear();
        XmlItems.Clear();

        JsonItems.SuppressOnChangedNotification = true;
        XmlItems.SuppressOnChangedNotification = true;

        foreach (var configurationSettingsFile in configurationSettingsFiles)
        {
            if (configurationSettingsFile.JsonSettings.Any())
            {
                JsonItems.Add(new ConfigurationSettingsJsonFileViewModel(configurationSettingsFile));
            }

            if (configurationSettingsFile.XmlSettings.Any())
            {
                XmlItems.Add(new ConfigurationSettingsXmlFileViewModel(configurationSettingsFile));
            }
        }

        JsonItems.SuppressOnChangedNotification = false;
        XmlItems.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(JsonItems)}.Count: {JsonItems?.Count}, {nameof(XmlItems)}.Count: {XmlItems?.Count}";
}