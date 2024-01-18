// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class ConfigurationSettingsJsonFileViewModel : ViewModelBase
{
    private readonly ComponentProviderViewModel? refComponentProviderViewModel;
    private string fileName = string.Empty;

    public ConfigurationSettingsJsonFileViewModel()
    {
    }

    public ConfigurationSettingsJsonFileViewModel(
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

    public ObservableCollectionEx<KeyValueTemplateItemViewModel> Settings { get; init; } = new();

    public void Populate(
        ConfigurationSettingsFileOption configurationSettingsFileOption)
    {
        ArgumentNullException.ThrowIfNull(configurationSettingsFileOption);

        FileName = configurationSettingsFileOption.FileName;

        Settings.Clear();

        Settings.SuppressOnChangedNotification = true;

        foreach (var keyValuePair in configurationSettingsFileOption.JsonSettings)
        {
            var value = keyValuePair.Value.ToString()!;
            if (value.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
            {
                if (refComponentProviderViewModel is null)
                {
                    continue;
                }

                var (resolvedValue, templateLocations) = refComponentProviderViewModel.ResolveValueAndTemplateLocations(value);

                if (templateLocations.Count > 0)
                {
                    Settings.Add(
                        new KeyValueTemplateItemViewModel(
                            keyValuePair.Key,
                            resolvedValue,
                            template: keyValuePair.Value.ToString(),
                            templateLocations));
                }
            }
            else
            {
                Settings.Add(
                    new KeyValueTemplateItemViewModel(
                        keyValuePair.Key,
                        keyValuePair.Value,
                        template: null,
                        templateLocations: null));
            }
        }

        Settings.SuppressOnChangedNotification = false;
    }

    public void ResolveValueAndTemplateLocations()
    {
        if (refComponentProviderViewModel is null)
        {
            return;
        }

        foreach (var keyValuePair in Settings)
        {
            var value = keyValuePair.Value.ToString()!;
            if (!value.ContainsTemplatePattern(TemplatePatternType.DoubleHardBrackets))
            {
                continue;
            }

            var (resolvedValue, templateLocations) = refComponentProviderViewModel.ResolveValueAndTemplateLocations(value);
            if (templateLocations.Count > 0)
            {
                keyValuePair.Value = resolvedValue;
            }
        }
    }

    public override string ToString()
        => $"{nameof(FileName)}: {FileName}, {nameof(Settings)}.Count: {Settings?.Count}";
}