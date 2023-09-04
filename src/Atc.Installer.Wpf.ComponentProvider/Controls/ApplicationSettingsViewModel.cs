// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class ApplicationSettingsViewModel : ViewModelBase
{
    private readonly ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders;
    private bool enableEditingMode;

    public ApplicationSettingsViewModel(
        ObservableCollectionEx<ComponentProviderViewModel> refComponentProviders)
    {
        this.refComponentProviders = refComponentProviders;
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
                            templateLocations: new List<string> { "DefaultApplicationSetting" }));
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

    public override string ToString()
        => string.Join("; ", Items);

    private void NewCommandHandler()
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

        //// TODO: Handle templates

        Items.Add(
            new KeyValueTemplateItemViewModel(
                data["Key"].ToString()!,
                data["Value"].ToString()!,
                template: null,
                templateLocations: null));

        IsDirty = true;
    }

    private void EditCommandHandler(
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
}