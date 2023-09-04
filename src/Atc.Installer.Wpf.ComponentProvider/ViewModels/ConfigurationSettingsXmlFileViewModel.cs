// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class ConfigurationSettingsXmlFileViewModel : ViewModelBase
{
    private string fileName = string.Empty;

    public ConfigurationSettingsXmlFileViewModel()
    {
    }

    public ConfigurationSettingsXmlFileViewModel(
        ComponentProviderViewModel refComponentProviderViewModel,
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        Populate(refComponentProviderViewModel,configurationSettingsFileOption);
    }

    public string FileName
    {
        get => fileName;
        set
        {
            fileName = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<XmlElementViewModel> Settings { get; init; } = new();

    public void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFileOption);

        FileName = configurationSettingsFileOption.FileName;

        Settings.Clear();

        Settings.SuppressOnChangedNotification = true;

        foreach (var xmlSetting in configurationSettingsFileOption.XmlSettings)
        {
            Settings.Add(new XmlElementViewModel(refComponentProviderViewModel, xmlSetting));
        }

        Settings.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(Settings)}.Count: {Settings?.Count}";
}