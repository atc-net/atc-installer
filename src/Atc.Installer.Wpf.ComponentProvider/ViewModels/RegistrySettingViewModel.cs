namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class RegistrySettingViewModel : ViewModelBase
{
    private string key = string.Empty;
    private InsertRemoveType action;

    public RegistrySettingViewModel()
    {
    }

    public RegistrySettingViewModel(
        RegistrySettingOption registrySetting)
    {
        ArgumentNullException.ThrowIfNull(registrySetting);

        Key = registrySetting.Key;
        Action = registrySetting.Action;
    }

    public string Key
    {
        get => key;
        set
        {
            key = value;
            RaisePropertyChanged();
        }
    }

    public InsertRemoveType Action
    {
        get => action;
        set
        {
            action = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Key)}: {Key}, {nameof(Action)}: {Action}";
}