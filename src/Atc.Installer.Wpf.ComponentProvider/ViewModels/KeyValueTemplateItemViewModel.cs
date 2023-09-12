// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class KeyValueTemplateItemViewModel : ViewModelBase
{
    private string key = string.Empty;
    private object value = default!;
    private string? template = string.Empty;

    public KeyValueTemplateItemViewModel()
    {
    }

    public KeyValueTemplateItemViewModel(
        string key,
        object value,
        string? template,
        IList<string>? templateLocations)
    {
        Key = key;
        Value = value;
        Template = template;
        if (templateLocations is not null)
        {
            TemplateLocations = new ObservableCollectionEx<string>();
            TemplateLocations.AddRange(templateLocations);
        }
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

    public object Value
    {
        get => value;
        set
        {
            this.value = value;
            RaisePropertyChanged();
        }
    }

    public string? Template
    {
        get => template;
        set
        {
            this.template = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollectionEx<string>? TemplateLocations { get; set; }

    public string GetValueAsString() => Value.ToString()!;

    public bool ContainsTemplateKey(
        string templateKey)
        => Template is not null &&
           Template.Contains(templateKey, StringComparison.Ordinal);

    public override string ToString()
    {
        if (Template is null || TemplateLocations is null)
        {
            return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
        }

        return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}, {nameof(Template)}: {Template}, {nameof(TemplateLocations)}: {string.Join(" # ", TemplateLocations!)}";
    }
}