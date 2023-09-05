// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable UseObjectOrCollectionInitializer
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

[SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
public class ConfigurationSettingsFilesViewModel : ViewModelBase
{
    private readonly ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders;
    private bool enableEditingMode;

    public ConfigurationSettingsFilesViewModel(
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders)
    {
        this.refComponentProviders = refComponentProviders;

        Messenger.Default.Register<UpdateEditingModeMessage>(this, HandleUpdateEditingModeMessage);
    }

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
        ArgumentNullException.ThrowIfNull(configurationSettingsFiles);

        JsonItems.Clear();
        XmlItems.Clear();

        JsonItems.SuppressOnChangedNotification = true;
        XmlItems.SuppressOnChangedNotification = true;

        foreach (var configurationSettingsFile in configurationSettingsFiles)
        {
            if (configurationSettingsFile.JsonSettings.Any())
            {
                JsonItems.Add(new ConfigurationSettingsJsonFileViewModel(refComponentProviderViewModel, configurationSettingsFile));
            }

            if (configurationSettingsFile.XmlSettings.Any())
            {
                XmlItems.Add(new ConfigurationSettingsXmlFileViewModel(refComponentProviderViewModel, configurationSettingsFile));
            }
        }

        JsonItems.SuppressOnChangedNotification = false;
        XmlItems.SuppressOnChangedNotification = false;
    }

    public override string ToString()
        => $"{nameof(JsonItems)}.Count: {JsonItems?.Count}, {nameof(XmlItems)}.Count: {XmlItems?.Count}";

    private void HandleUpdateEditingModeMessage(
        UpdateEditingModeMessage obj)
    {
        EnableEditingMode = obj.EnableEditingMode;
    }

    private void NewJsonCommandHandler(
        ConfigurationSettingsJsonFileViewModel item)
    {
        var labelControls = new List<ILabelControlBase>
        {
            new LabelTextBox
            {
                LabelText = "Key",
                IsMandatory = true,
                MinLength = 1,
            },
            new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = true,
                MinLength = 1,
            },
        };

        //// TODO: Handle templates

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

        var configurationSettingsJsonFile = JsonItems.First(x => x.FileName.Equals(item.FileName, StringComparison.Ordinal));

        //// TODO: Handle templates

        configurationSettingsJsonFile.Settings.Add(
            new KeyValueTemplateItemViewModel(
                data["Key"].ToString()!,
                data["Value"].ToString()!,
                template: null,
                templateLocations: null));

        IsDirty = true;
    }

    private void EditJsonCommandHandler(
        KeyValueTemplateItemViewModel item)
    {
        var labelControls = new List<ILabelControlBase>
        {
            new LabelTextBox
            {
                LabelText = "Key",
                IsEnabled = false,
                Text = item.Key,
            },
            new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = true,
                MinLength = 1,
                Text = item.Value.ToString()!,
            },
        };

        //// TODO: Handle templates

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

        var data = dialogBox.Data.GetKeyValues();

        //// TODO: Handle templates

        item.Value = data["Value"];

        IsDirty = true;
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
        var labelControls = new List<ILabelControlBase>
        {
            new LabelTextBox
            {
                LabelText = "Key",
                IsMandatory = true,
                MinLength = 1,
            },
            new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = true,
                MinLength = 1,
            },
        };

        //// TODO: Handle templates

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

        var configurationSettingsXmlFile = XmlItems.First(x => x.FileName.Equals(item.FileName, StringComparison.Ordinal));

        //// TODO: Handle templates

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
                data["Value"].ToString()!,
                template: null,
                templateLocations: null),
        };

        configurationSettingsXmlFile.Settings.Add(xmlElementViewModel);

        IsDirty = true;
    }

    private void EditXmlCommandHandler(
        XmlElementViewModel item)
    {
        var labelControls = new List<ILabelControlBase>
        {
            new LabelTextBox
            {
                LabelText = "Key",
                IsEnabled = false,
                Text = item.Attributes.First(x => x.Key == "key").GetValueAsString(),
            },
            new LabelTextBox
            {
                LabelText = "Value",
                IsMandatory = true,
                MinLength = 1,
                Text = item.Attributes.First(x => x.Key == "value").GetValueAsString(),
            },
        };

        //// TODO: Handle templates

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

        var data = dialogBox.Data.GetKeyValues();

        //// TODO: Handle templates

        var attribute = item.Attributes.First(x => x.Key == "value");
        attribute.Value = data["Value"];

        IsDirty = true;
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
}