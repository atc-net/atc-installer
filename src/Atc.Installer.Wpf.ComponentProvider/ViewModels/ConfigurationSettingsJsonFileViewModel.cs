namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class ConfigurationSettingsJsonFileViewModel : ViewModelBase
{
    private string fileName = string.Empty;

    public ConfigurationSettingsJsonFileViewModel()
    {
    }

    public ConfigurationSettingsJsonFileViewModel(
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        Populate(configurationSettingsFileOption);
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

    public ObservableCollectionEx<KeyValueItemViewModel> Settings { get; init; } = new();

    public void Populate(
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFileOption);

        FileName = configurationSettingsFileOption.FileName;

        Settings.Clear();

        Settings.SuppressOnChangedNotification = true;

        foreach (var keyValuePair in configurationSettingsFileOption.JsonSettings)
        {
            Settings.Add(new KeyValueItemViewModel(keyValuePair));
        }

        Settings.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(Settings)}.Count: {Settings?.Count}";
}