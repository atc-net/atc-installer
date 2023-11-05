// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable ArrangeTrailingCommaInMultilineLists
// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

[SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "OK.")]
[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public class ConfigurationSettingsFilesViewModel : ViewModelBase
{
    private ComponentProviderViewModel? refComponentProvider;
    private bool enableEditingMode;

    public ConfigurationSettingsFilesViewModel()
        => Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);

    public IRelayCommand<ConfigurationSettingsJsonFileViewModel> NewJsonCommand
        => new RelayCommand<ConfigurationSettingsJsonFileViewModel>(
            NewJsonCommandHandler);

    public IRelayCommand<KeyValueTemplateItemViewModel> EditJsonCommand
        => new RelayCommand<KeyValueTemplateItemViewModel>(
            EditJsonCommandHandler);

    public IRelayCommand<KeyValueTemplateItemViewModel> DeleteJsonCommand
        => new RelayCommand<KeyValueTemplateItemViewModel>(
            DeleteJsonCommandHandler);

    public IRelayCommand<ConfigurationSettingsXmlFileViewModel> NewXmlCommand
        => new RelayCommand<ConfigurationSettingsXmlFileViewModel>(
            NewXmlCommandHandler);

    public IRelayCommand<XmlElementViewModel> EditXmlCommand
        => new RelayCommand<XmlElementViewModel>(
            EditXmlCommandHandler);

    public IRelayCommand<XmlElementViewModel> DeleteXmlCommand
        => new RelayCommand<XmlElementViewModel>(
            DeleteXmlCommandHandler);

    public bool EnableEditingMode
    {
        get => enableEditingMode;
        set
        {
            if (enableEditingMode == value)
            {
                return;
            }

            enableEditingMode = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<ConfigurationSettingsJsonFileViewModel> JsonItems { get; init; } = new();

    public ObservableCollectionEx<ConfigurationSettingsXmlFileViewModel> XmlItems { get; init; } = new();

    public void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        IList<ConfigurationSettingsFileOption> configurationSettingsFiles)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(configurationSettingsFiles);

        refComponentProvider = refComponentProviderViewModel;

        JsonItems.Clear();
        XmlItems.Clear();

        JsonItems.SuppressOnChangedNotification = true;
        XmlItems.SuppressOnChangedNotification = true;

        foreach (var configurationSettingsFile in configurationSettingsFiles)
        {
            if (configurationSettingsFile.JsonSettings.Any())
            {
                JsonItems.Add(
                    new ConfigurationSettingsJsonFileViewModel(
                        refComponentProviderViewModel,
                        configurationSettingsFile));
            }

            if (configurationSettingsFile.XmlSettings.Any())
            {
                XmlItems.Add(
                    new ConfigurationSettingsXmlFileViewModel(
                        refComponentProviderViewModel,
                        configurationSettingsFile));
            }
        }

        JsonItems.SuppressOnChangedNotification = false;
        XmlItems.SuppressOnChangedNotification = false;
    }

    public void ClearAllIsDirty()
    {
        IsDirty = false;
        foreach (var item in JsonItems)
        {
            item.IsDirty = false;
        }

        foreach (var item in XmlItems)
        {
            item.IsDirty = false;
        }
    }

    public void ResolveValueAndTemplateReferences()
    {
        JsonItems.SuppressOnChangedNotification = true;
        XmlItems.SuppressOnChangedNotification = true;

        foreach (var jsonItem in JsonItems)
        {
            jsonItem.ResolveValueAndTemplateLocations();
        }

        foreach (var xmlItem in XmlItems)
        {
            xmlItem.ResolveValueAndTemplateLocations();
        }

        JsonItems.SuppressOnChangedNotification = false;
        XmlItems.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(JsonItems)}.Count: {JsonItems?.Count}, {nameof(XmlItems)}.Count: {XmlItems?.Count}";

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => EnableEditingMode = obj.EnableEditingMode;

    private void NewJsonCommandHandler(
        ConfigurationSettingsJsonFileViewModel item)
    {
        var dialogBox = InputFormDialogBoxFactory.CreateForNewConfigurationSettingsFiles(
            refComponentProvider);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var items = JsonItems
            .First(x => x.FileName.Equals(item.FileName, StringComparison.Ordinal))
            .Settings;

        var data = dialogBox.Data.GetKeyValues();

        var dataKey = data["Key"].ToString()!;

        if (items.Any(x => x.Key.Equals(dataKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var dataValue = data["Value"].ToString()!;
        var dataTemplate = string.Empty;
        if (data.TryGetValue("Templates", out var value))
        {
            dataTemplate = value?.ToString()!;
        }

        if (string.IsNullOrEmpty(dataValue) &&
            (string.IsNullOrEmpty(dataTemplate) ||
             dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
        {
            return;
        }

        if (string.IsNullOrEmpty(dataTemplate) ||
            dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
        {
            items.Add(
                new KeyValueTemplateItemViewModel(
                    dataKey,
                    dataValue,
                    template: null,
                    templateLocations: null));
        }
        else
        {
            (dataTemplate, var templateLocation, var dataDefaultValue) = TemplateExtract(dataTemplate);

            items.Add(
                new KeyValueTemplateItemViewModel(
                    dataKey,
                    dataDefaultValue,
                    template: $"[[{dataTemplate}]]",
                    templateLocations: new List<string> { templateLocation }));
        }

        IsDirty = true;
    }

    private void EditJsonCommandHandler(
        KeyValueTemplateItemViewModel item)
    {
        var updateItem = JsonItems[0].Settings.First(x => x.Key.Equals(item.Key, StringComparison.Ordinal));

        var dialogBox = InputFormDialogBoxFactory.CreateForEditConfigurationSettingsFiles(
            refComponentProvider,
            updateItem);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        HandleEditDialogResult(dialogBox, updateItem);
    }

    private void DeleteJsonCommandHandler(
        KeyValueTemplateItemViewModel item)
    {
        var dialogBox = new QuestionDialogBox(
            Application.Current.MainWindow!,
            "Delete key/value",
            $"Are you sure you want to delete:\n\n{item.Key}")
        {
            Width = 500,
        };

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var removeItem = JsonItems
            .Select(jsonItem => jsonItem.Settings.FirstOrDefault(x => x.Key == item.Key))
            .FirstOrDefault(x => x is not null);

        if (removeItem is not null)
        {
            foreach (var jsonItem in JsonItems)
            {
                jsonItem.Settings.Remove(removeItem);
            }
        }

        IsDirty = true;
    }

    private void NewXmlCommandHandler(
        ConfigurationSettingsXmlFileViewModel item)
    {
        var dialogBox = InputFormDialogBoxFactory.CreateForNewConfigurationSettingsFiles(
            refComponentProvider);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var items = XmlItems
            .First(x => x.FileName.Equals(item.FileName, StringComparison.Ordinal))
            .Settings;

        var data = dialogBox.Data.GetKeyValues();

        var dataKey = data["Key"].ToString()!;

        if (items
            .Select(xe => xe.Attributes.First(x => x.Key.Equals("key", StringComparison.Ordinal)))
            .Any(kv => kv.GetValueAsString().Equals(dataKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var dataValue = data["Value"].ToString()!;
        var dataTemplate = string.Empty;
        if (data.TryGetValue("Templates", out var value))
        {
            dataTemplate = value?.ToString()!;
        }

        if (string.IsNullOrEmpty(dataValue) &&
            (string.IsNullOrEmpty(dataTemplate) ||
             dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
        {
            return;
        }

        if (string.IsNullOrEmpty(dataTemplate) ||
            dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
        {
            var xmlElementViewModel = new XmlElementViewModel(
                new XmlElementSettingsOptions { Path = "configuration:appSettings", Element = "add", });

            xmlElementViewModel.Attributes = new ObservableCollectionEx<KeyValueTemplateItemViewModel>
            {
                new(
                    "key",
                    data["Key"].ToString()!,
                    template: null,
                    templateLocations: null),
                new(
                    "value",
                    data["Value"].ToString()!,
                    template: null,
                    templateLocations: null),
            };

            items.Add(xmlElementViewModel);
        }
        else
        {
            (dataTemplate, var templateLocation, var dataDefaultValue) = TemplateExtract(dataTemplate);

            var xmlElementViewModel = new XmlElementViewModel(
                new XmlElementSettingsOptions
                {
                    Path = "configuration:appSettings",
                    Element = "add",
                });

            xmlElementViewModel.Attributes = new ObservableCollectionEx<KeyValueTemplateItemViewModel>
            {
                new(
                    "key",
                    data["Key"].ToString()!,
                    template: null,
                    templateLocations: null),
                new(
                    "value",
                    dataDefaultValue,
                    template: $"[[{dataTemplate}]]",
                    templateLocations: new List<string> { templateLocation }),
            };

            items.Add(xmlElementViewModel);
        }

        IsDirty = true;
    }

    private void EditXmlCommandHandler(
        XmlElementViewModel item)
    {
        KeyValueTemplateItemViewModel? updateItem = null;
        foreach (var xmlElement in XmlItems[0].Settings)
        {
            if (xmlElement != item)
            {
                continue;
            }

            updateItem = xmlElement.Attributes.First(x => x.Key.Equals("value", StringComparison.Ordinal));
            updateItem.Key = xmlElement.Attributes.First(x => x.Key.Equals("key", StringComparison.Ordinal)).GetValueAsString();
        }

        if (updateItem is null)
        {
            return;
        }

        var dialogBox = InputFormDialogBoxFactory.CreateForEditConfigurationSettingsFiles(
            refComponentProvider,
            updateItem);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        HandleEditDialogResult(dialogBox, updateItem);
    }

    private void DeleteXmlCommandHandler(
        XmlElementViewModel item)
    {
        var itemKey = item.Attributes.First(x => x.Key == "key").GetValueAsString();

        var dialogBox = new QuestionDialogBox(
            Application.Current.MainWindow!,
            "Delete key/value",
            $"Are you sure you want to delete:\n\n{itemKey}")
        {
            Width = 500,
        };

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        XmlElementViewModel? removeItem = null;
        foreach (var xmlItem in XmlItems)
        {
            if (removeItem is not null)
            {
                break;
            }

            foreach (var xmlElement in xmlItem.Settings)
            {
                if (removeItem is not null)
                {
                    break;
                }

                if (xmlElement.Attributes.Any(a => a.Key == item.Attributes.First(x => x.Key == "key").Key))
                {
                    removeItem = xmlElement;
                }
            }
        }

        if (removeItem is not null)
        {
            foreach (var xmlItem in XmlItems)
            {
                xmlItem.Settings.Remove(removeItem);
            }
        }

        IsDirty = true;
    }

    private void HandleEditDialogResult(
        InputFormDialogBox dialogBox,
        KeyValueTemplateItemViewModel updateItem)
    {
        var oldValue = updateItem.GetValueAsString();

        var data = dialogBox.Data.GetKeyValues();

        var dataValue = data["Value"].ToString()!;

        var dataTemplate = string.Empty;
        if (data.TryGetValue("Templates", out var value))
        {
            dataTemplate = value?.ToString()!;
        }

        if (string.IsNullOrEmpty(dataValue) &&
            (string.IsNullOrEmpty(dataTemplate) ||
             dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
        {
            return;
        }

        if (string.IsNullOrEmpty(dataTemplate) ||
            dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
        {
            updateItem.Value = dataValue;
            updateItem.Template = null;
            updateItem.TemplateLocations = null;
        }
        else
        {
            (dataTemplate, var templateLocation, var dataDefaultValue) = TemplateExtract(dataTemplate);

            updateItem.Value = dataDefaultValue;
            updateItem.Template = $"[[{dataTemplate}]]";
            updateItem.TemplateLocations = new ObservableCollectionEx<string>
            {
                templateLocation,
            };
        }

        UpdateComponentProviders(oldValue, updateItem);

        IsDirty = true;
    }

    private void UpdateComponentProviders(
        string oldValue,
        KeyValueTemplateItemViewModel updateItem)
    {
        if (refComponentProvider is null ||
            oldValue is null)
        {
            return;
        }

        var value = updateItem.GetValueAsString();
        if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var endpoint in refComponentProvider.Endpoints)
            {
                if (endpoint.Endpoint.StartsWith(oldValue, StringComparison.OrdinalIgnoreCase))
                {
                    endpoint.Endpoint = endpoint.Endpoint.Replace(oldValue, value, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private (string DataTemplate, string TemplateLocation, string DataDefaultValue) TemplateExtract(
        string dataTemplate)
    {
        var templateLocation = string.Empty;
        var dataDefaultValue = string.Empty;

        if (refComponentProvider is null)
        {
            return (dataTemplate, templateLocation, dataDefaultValue);
        }

        var sa = dataTemplate.Split('|');
        dataTemplate = sa[^1];

        if (sa[0].Equals(Constants.Default, StringComparison.Ordinal))
        {
            templateLocation = Constants.DefaultTemplateLocation;
            dataDefaultValue = refComponentProvider.DefaultApplicationSettings.Items
                .First(x => x.Key.Equals(dataTemplate, StringComparison.Ordinal))
                .GetValueAsString();
        }
        else if (sa[0].Equals(Constants.Current, StringComparison.Ordinal))
        {
            templateLocation = Constants.CurrentTemplateLocation;
            dataDefaultValue = refComponentProvider.ApplicationSettings.Items
                .First(x => x.Key.Equals(dataTemplate, StringComparison.Ordinal))
                .GetValueAsString();
        }

        return (dataTemplate, templateLocation, dataDefaultValue);
    }
}