// ReSharper disable InvertIf
// ReSharper disable MergeSequentialChecks
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public class ApplicationSettingsViewModel : ViewModelBase
{
    private readonly ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders;
    private readonly bool isDefaultApplicationSettings;
    private bool enableEditingMode;
    private ApplicationSettingsViewModel? defaultSettings;

    public ApplicationSettingsViewModel(
        bool isDefaultApplicationSettings,
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders)
    {
        this.isDefaultApplicationSettings = isDefaultApplicationSettings;
        this.refComponentProviders = refComponentProviders;

        Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);
    }

    public IRelayCommand NewCommand
        => new RelayCommand(
            NewCommandHandler);

    public IRelayCommand<KeyValueTemplateItemViewModel> EditCommand
        => new RelayCommand<KeyValueTemplateItemViewModel>(
            EditCommandHandler);

    public IRelayCommand<KeyValueTemplateItemViewModel> DeleteCommand
        => new RelayCommand<KeyValueTemplateItemViewModel>(
            DeleteCommandHandler,
            CanDeleteCommandHandler);

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

    public ObservableCollectionEx<KeyValueTemplateItemViewModel> Items { get; init; } = new();

    public void Populate(
        ObservableCollectionEx<KeyValueTemplateItemViewModel> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var applicationSetting in applicationSettings)
        {
            Items.Add(
                new KeyValueTemplateItemViewModel(
                    applicationSetting.Key,
                    applicationSetting.Value,
                    template: null,
                    templateLocations: null));
        }

        Items.SuppressOnChangedNotification = false;
    }

    public void Populate(
        ApplicationSettingsViewModel defaultApplicationSettings,
        IDictionary<string, object> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(defaultApplicationSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings);

        this.defaultSettings = defaultApplicationSettings;

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var applicationSetting in applicationSettings)
        {
            var value = applicationSetting.Value.ToString()!;
            if (applicationSetting.Value.ToString()!.ContainsTemplateKeyBrackets())
            {
                var key = value.GetTemplateKeys()[0];
                if (defaultApplicationSettings.TryGetString(key, out var resultValue))
                {
                    Items.Add(
                        new KeyValueTemplateItemViewModel(
                            applicationSetting.Key,
                            resultValue,
                            value,
                            templateLocations: new List<string> { Constants.DefaultTemplateLocation }));
                }
            }
            else
            {
                Items.Add(
                    new KeyValueTemplateItemViewModel(
                        applicationSetting.Key,
                        applicationSetting.Value,
                        template: null,
                        templateLocations: null));
            }
        }

        Items.SuppressOnChangedNotification = false;
    }

    public bool TryGetString(string key, out string value)
    {
        var item = Items.FirstOrDefault(x => x.Key == key);
        if (item?.Value is null)
        {
            value = string.Empty;
            return false;
        }

        value = item.Value.ToString()!;
        return true;
    }

    public bool TryGetBoolean(string key, out bool value)
    {
        var item = Items.FirstOrDefault(x => x.Key == key);
        if (item?.Value is null)
        {
            value = default;
            return false;
        }

        if (bool.TryParse(
                item.Value.ToString()!,
                out var resultValue))
        {
            value = resultValue;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetUshort(string key, out ushort value)
    {
        var item = Items.FirstOrDefault(x => x.Key == key);
        if (item?.Value is null)
        {
            value = default;
            return false;
        }

        if (ushort.TryParse(
                item.Value.ToString()!,
                NumberStyles.Any,
                GlobalizationConstants.EnglishCultureInfo,
                out var resultValue))
        {
            value = resultValue;
            return true;
        }

        value = default;
        return false;
    }

    public void ClearAllIsDirty()
    {
        IsDirty = false;
        foreach (var item in Items)
        {
            item.IsDirty = false;
        }
    }

    public override string ToString()
        => string.Join("; ", Items);

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => EnableEditingMode = obj.EnableEditingMode;

    private void NewCommandHandler()
    {
        var labelControls = CreateLabelControls(updateItem: null);
        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        var dialogBox = new InputFormDialogBox(
            Application.Current.MainWindow!,
            "New key/value",
            labelControlsForm);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        var data = dialogBox.Data.GetKeyValues();

        var dataKey = data["Key"].ToString()!;
        var dataValue = data["Value"].ToString()!;

        if (Items.Any(x => x.Key.Equals(dataKey, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (isDefaultApplicationSettings)
        {
            Items.Add(
                new KeyValueTemplateItemViewModel(
                    dataKey,
                    dataValue,
                    template: null,
                    templateLocations: null));
        }
        else
        {
            var dataTemplate = data["Templates"].ToString()!;

            if (string.IsNullOrEmpty(dataValue) &&
                (string.IsNullOrEmpty(dataTemplate) ||
                dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
            {
                return;
            }

            if (string.IsNullOrEmpty(dataTemplate) ||
                dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
            {
                Items.Add(
                    new KeyValueTemplateItemViewModel(
                        dataKey,
                        dataValue,
                        template: null,
                        templateLocations: null));
            }
            else
            {
                var dataDefaultValue = defaultSettings is null
                    ? dataValue
                    : defaultSettings.Items
                        .First(x => x.Key.Equals(dataTemplate, StringComparison.OrdinalIgnoreCase))
                        .GetValueAsString();

                Items.Add(
                    new KeyValueTemplateItemViewModel(
                        dataKey,
                        dataDefaultValue,
                        template: $"[[{dataTemplate}]]",
                        templateLocations: new List<string> { Constants.DefaultTemplateLocation }));
            }
        }

        IsDirty = true;
    }

    private void EditCommandHandler(
        KeyValueTemplateItemViewModel item)
    {
        var updateItem = Items.First(x => x.Key.Equals(item.Key, StringComparison.Ordinal));

        var labelControls = CreateLabelControls(updateItem);
        var labelControlsForm = new LabelControlsForm();
        labelControlsForm.AddColumn(labelControls);

        var dialogBox = new InputFormDialogBox(
            Application.Current.MainWindow!,
            "Edit key/value",
            labelControlsForm);

        dialogBox.ShowDialog();

        if (!dialogBox.DialogResult.HasValue ||
            !dialogBox.DialogResult.Value)
        {
            return;
        }

        HandleEditDialogResult(dialogBox, updateItem);
    }

    private bool CanDeleteCommandHandler(
        KeyValueTemplateItemViewModel item)
    {
        var templateKey = $"[[{item.Key}]]";

        foreach (var componentProvider in refComponentProviders)
        {
            if (componentProvider.ApplicationSettings.Items.Any(x => x.ContainsTemplateKey(templateKey)))
            {
                return false;
            }

            if (componentProvider.ConfigurationSettingsFiles.JsonItems
                .Any(jsonItem => jsonItem.Settings.Any(x => x.ContainsTemplateKey(templateKey))))
            {
                return false;
            }

            if (componentProvider.ConfigurationSettingsFiles.XmlItems
                .SelectMany(xmlItem => xmlItem.Settings)
                .Any(xmlItem => xmlItem.Attributes.Any(x => x.ContainsTemplateKey(templateKey))))
            {
                return false;
            }

            if (componentProvider.Endpoints.Any(x => x.ContainsTemplateKey(templateKey)))
            {
                return false;
            }
        }

        return true;
    }

    private void DeleteCommandHandler(
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

        Items.Remove(item);

        IsDirty = true;
    }

    private List<ILabelControlBase> CreateLabelControls(
        KeyValueTemplateItemViewModel? updateItem)
    {
        var labelControls = new List<ILabelControlBase>();

        var labelTextBoxKey = new LabelTextBox
        {
            LabelText = "Key",
            IsMandatory = true,
            MinLength = 1,
        };

        if (updateItem is not null)
        {
            labelTextBoxKey.IsMandatory = false;
            labelTextBoxKey.IsEnabled = false;
            labelTextBoxKey.Text = updateItem.Key;
        }

        labelControls.Add(labelTextBoxKey);

        if (defaultSettings is null)
        {
            var labelTextBoxValue = new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = true,
                MinLength = 1,
            };

            if (updateItem is not null)
            {
                labelTextBoxValue.Text = string.IsNullOrEmpty(updateItem.Template)
                    ? updateItem.Value.ToString()!
                    : updateItem.Template;
            }

            labelControls.Add(labelTextBoxValue);
        }
        else
        {
            var labelTextBoxValue = new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = false,
            };

            var labelComboBox = new LabelComboBox
            {
                LabelText = "Templates",
                IsMandatory = false,
                Items = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    {
                        DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString(),
                        string.Empty
                    },
                },
            };

            foreach (var keyValueTemplateItem in defaultSettings.Items)
            {
                labelComboBox.Items.Add(
                    keyValueTemplateItem.Key,
                    $"[[{keyValueTemplateItem.Key}]]   -   {keyValueTemplateItem.GetValueAsString()}");
            }

            if (updateItem is not null)
            {
                if (string.IsNullOrEmpty(updateItem.Template))
                {
                    labelTextBoxValue.Text = updateItem.Value.ToString()!;
                }
                else
                {
                    labelComboBox.SelectedKey = updateItem.Template.GetTemplateKeys()[0];
                }
            }

            labelControls.Add(labelTextBoxValue);
            labelControls.Add(labelComboBox);

            labelTextBoxValue.TextChanged += (_, args) =>
            {
                if (args.NewValue is not null &&
                    args.OldValue != args.NewValue)
                {
                    labelComboBox.SelectedKey = labelComboBox.Items.First().Key;
                }
            };

            labelComboBox.SelectorChanged += (_, args) =>
            {
                if (args.NewValue is not null &&
                    args.NewValue != DropDownFirstItemTypeHelper.GetEnumGuid(DropDownFirstItemType.Blank).ToString())
                {
                    labelTextBoxValue.Text = string.Empty;
                }
            };
        }

        return labelControls;
    }

    private void HandleEditDialogResult(
        InputFormDialogBox dialogBox,
        KeyValueTemplateItemViewModel updateItem)
    {
        var data = dialogBox.Data.GetKeyValues();

        var dataValue = data["Value"].ToString()!;

        if (isDefaultApplicationSettings)
        {
            if (string.IsNullOrEmpty(dataValue))
            {
                return;
            }

            updateItem.Value = dataValue;
            updateItem.Template = null;
            updateItem.TemplateLocations = null;

            UpdateComponentProviders(updateItem);
        }
        else
        {
            var dataTemplate = data["Templates"].ToString()!;

            if (string.IsNullOrEmpty(dataValue) &&
                (string.IsNullOrEmpty(dataTemplate) ||
                 dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal)))
            {
                return;
            }

            if (string.IsNullOrEmpty(dataTemplate) ||
                dataTemplate.Equals(Constants.ItemBlankIdentifier, StringComparison.Ordinal))
            {
                updateItem.Value = data["Value"].ToString()!;
                updateItem.Template = null;
                updateItem.TemplateLocations = null;
            }
            else
            {
                var dataDefaultValue = defaultSettings is null
                    ? dataValue
                    : defaultSettings.Items
                        .First(x => x.Key.Equals(dataTemplate, StringComparison.OrdinalIgnoreCase))
                        .GetValueAsString();

                updateItem.Value = dataDefaultValue;
                updateItem.Template = $"[[{dataTemplate}]]";
                updateItem.TemplateLocations = new ObservableCollectionEx<string>
                {
                    Constants.DefaultTemplateLocation,
                };
            }

            UpdateComponentProviders(updateItem);
        }

        IsDirty = true;
    }

    private void UpdateComponentProviders(
        KeyValueTemplateItemViewModel updateItem)
    {
        foreach (var componentProvider in refComponentProviders)
        {
            if (componentProvider.InstalledMainFilePath is not null &&
                componentProvider.InstalledMainFilePath.Template is not null &&
                componentProvider.InstalledMainFilePath.TemplateLocations is not null &&
                componentProvider.InstalledMainFilePath.Template.Contains(updateItem.Key, StringComparison.Ordinal))
            {
                componentProvider.InstalledMainFilePath.Value = ResolveTemplateValue(updateItem, componentProvider, componentProvider.InstalledMainFilePath.Template);
            }

            if (componentProvider.InstallationFolderPath is not null &&
                componentProvider.InstallationFolderPath.Template is not null &&
                componentProvider.InstallationFolderPath.TemplateLocations is not null &&
                componentProvider.InstallationFolderPath.Template.Contains(updateItem.Key, StringComparison.Ordinal))
            {
                componentProvider.InstallationFolderPath.Value = ResolveTemplateValue(updateItem, componentProvider, componentProvider.InstallationFolderPath.Template);
            }

            foreach (var item in componentProvider.ApplicationSettings.Items)
            {
                if (item.Template is not null &&
                    item.TemplateLocations is not null &&
                    item.Template.Contains(updateItem.Key, StringComparison.Ordinal))
                {
                    item.Value = ResolveTemplateValue(updateItem, componentProvider, item.Template);
                }
            }

            foreach (var item in componentProvider.BrowserLinkEndpoints)
            {
                if (item.Template is not null &&
                    item.TemplateLocations is not null &&
                    item.Template.Contains(updateItem.Key, StringComparison.Ordinal))
                {
                    item.Endpoint = ResolveTemplateValue(updateItem, componentProvider, item.Template);
                }
            }

            foreach (var item in componentProvider.ConfigurationSettingsFiles.JsonItems)
            {
                foreach (var keyValueTemplateItem in item.Settings)
                {
                    if (keyValueTemplateItem.Template is not null &&
                        keyValueTemplateItem.TemplateLocations is not null &&
                        keyValueTemplateItem.Template.Contains(updateItem.Key, StringComparison.Ordinal))
                    {
                        keyValueTemplateItem.Value = ResolveTemplateValue(updateItem, componentProvider, keyValueTemplateItem.Template);
                    }
                }
            }

            foreach (var item in componentProvider.ConfigurationSettingsFiles.XmlItems)
            {
                foreach (var xmlElement in item.Settings)
                {
                    foreach (var keyValueTemplateItem in xmlElement.Attributes)
                    {
                        if (keyValueTemplateItem.Template is not null &&
                            keyValueTemplateItem.TemplateLocations is not null &&
                            keyValueTemplateItem.Template.Contains(updateItem.Key, StringComparison.Ordinal))
                        {
                            keyValueTemplateItem.Value = ResolveTemplateValue(updateItem, componentProvider, keyValueTemplateItem.Template);
                        }
                    }
                }
            }
        }
    }

    private static string ResolveTemplateValue(
        KeyValueTemplateItemViewModel updateItem,
        ComponentProviderViewModel componentProvider,
        string template)
    {
        var terms = template.SplitTemplate();
        var sb = new StringBuilder();
        foreach (var term in terms)
        {
            if (term.Equals(updateItem.Key, StringComparison.Ordinal))
            {
                sb.Append(updateItem.Value);
            }
            else
            {
                if (componentProvider.TryGetStringFromApplicationSetting(term, out var strValue))
                {
                    sb.Append(strValue);
                }
                else if (componentProvider.TryGetUshortFromApplicationSettings(term, out var ushortValue))
                {
                    sb.Append(ushortValue);
                }
                else
                {
                    sb.Append(term);
                }
            }
        }

        return sb.ToString();
    }
}