// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class ConfigurationSettingsXmlFileViewModel : ViewModelBase
{
    private readonly ComponentProviderViewModel? refComponentProviderViewModel;
    private string fileName = string.Empty;

    public ConfigurationSettingsXmlFileViewModel(
        ComponentProviderViewModel refComponentProviderViewModel,
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(configurationSettingsFileOption);

        this.refComponentProviderViewModel = refComponentProviderViewModel;

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

    public ObservableCollectionEx<XmlElementViewModel> Settings { get; init; } = new();

    public void Populate(
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFileOption);

        FileName = configurationSettingsFileOption.FileName;

        Settings.Clear();

        if (refComponentProviderViewModel is null)
        {
            return;
        }

        Settings.SuppressOnChangedNotification = true;

        foreach (var xmlSetting in configurationSettingsFileOption.XmlSettings)
        {
            Settings.Add(new XmlElementViewModel(refComponentProviderViewModel, xmlSetting));
        }

        Settings.SuppressOnChangedNotification = false;
    }

    public void ResolveValueAndTemplateLocations()
    {
        if (refComponentProviderViewModel is null)
        {
            return;
        }

        foreach (var xmlItem in Settings)
        {
            var attributeItem = xmlItem.Attributes.FirstOrDefault(x => x.Key == "value");
            if (attributeItem is null)
            {
                continue;
            }

            var value = attributeItem.Value?.ToString()!;
            if (!value.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
            {
                continue;
            }

            var (resolvedValue, templateLocations) = refComponentProviderViewModel.ResolveValueAndTemplateLocations(value);
            if (templateLocations.Count > 0)
            {
                attributeItem.Value = resolvedValue;
            }
        }
    }

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(Settings)}.Count: {Settings?.Count}";
}