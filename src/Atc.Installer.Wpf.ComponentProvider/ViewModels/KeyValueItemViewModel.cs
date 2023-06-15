namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class KeyValueItemViewModel : ViewModelBase
{
    private string key = string.Empty;
    private object? value;

    public KeyValueItemViewModel()
    {
    }

    public KeyValueItemViewModel(
        KeyValuePair<string, object> keyValuePair)
    {
        Key = keyValuePair.Key;
        Value = keyValuePair.Value;
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

    public object? Value
    {
        get => value;
        set
        {
            this.value = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
}