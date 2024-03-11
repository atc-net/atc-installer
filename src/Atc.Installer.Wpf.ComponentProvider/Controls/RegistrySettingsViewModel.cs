namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class RegistrySettingsViewModel : ViewModelBase
{
    private ComponentProviderViewModel? refComponentProvider;
    private bool enableEditingMode;

    public RegistrySettingsViewModel()
        => Messenger.Default.Register<UpdateApplicationOptionsMessage>(this, HandleUpdateApplicationOptionsMessage);

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

    public ObservableCollectionEx<RegistrySettingViewModel> Items { get; init; } = new();

    public void Populate(
        ComponentProviderViewModel refComponentProviderViewModel,
        IList<RegistrySettingOption> registrySettings)
    {
        ArgumentNullException.ThrowIfNull(refComponentProviderViewModel);
        ArgumentNullException.ThrowIfNull(registrySettings);

        refComponentProvider = refComponentProviderViewModel;

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var registrySetting in registrySettings)
        {
            Items.Add(new RegistrySettingViewModel(registrySetting));
        }

        Items.SuppressOnChangedNotification = false;
    }

    public void ClearAllIsDirty()
    {
        IsDirty = false;
        foreach (var item in Items)
        {
            item.IsDirty = false;
        }
    }

    private void HandleUpdateApplicationOptionsMessage(
        UpdateApplicationOptionsMessage obj)
        => EnableEditingMode = obj.EnableEditingMode;
}