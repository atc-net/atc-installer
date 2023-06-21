namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class ApplicationSettingsViewModel : ViewModelBase
{
    public ObservableCollectionEx<KeyValueItemViewModel> Items { get; init; } = new();

    public void Populate(
        IDictionary<string, object> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);

        Items.SuppressOnChangedNotification = true;

        Items.Clear();

        foreach (var applicationSetting in applicationSettings)
        {
            Items.Add(new KeyValueItemViewModel(applicationSetting));
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
}