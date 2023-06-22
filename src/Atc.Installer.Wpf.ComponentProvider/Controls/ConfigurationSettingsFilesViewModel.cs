namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class ConfigurationSettingsFilesViewModel : ViewModelBase
{
    public ObservableCollectionEx<ConfigurationSettingsJsonFileViewModel> JsonItems { get; init; } = new();

    public ObservableCollectionEx<ConfigurationSettingsXmlFileViewModel> XmlItems { get; init; } = new();

    public void Populate(
        IList<ConfigurationSettingsFileOption> configurationSettingsFiles)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFiles);

        JsonItems.SuppressOnChangedNotification = true;
        XmlItems.SuppressOnChangedNotification = true;

        JsonItems.Clear();
        XmlItems.Clear();

        foreach (var configurationSettingsFile in configurationSettingsFiles)
        {
            if (configurationSettingsFile.JsonSettings.Any())
            {
                // TODO: Implement
            }

            if (configurationSettingsFile.XmlSettings.Any())
            {
                // TODO: Implement
            }
        }

        JsonItems.SuppressOnChangedNotification = false;
        XmlItems.SuppressOnChangedNotification = false;
    }
}