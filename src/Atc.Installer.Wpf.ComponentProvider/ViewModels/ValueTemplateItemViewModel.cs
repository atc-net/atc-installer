// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class ValueTemplateItemViewModel : ViewModelBase
{
    private object? value;
    private string? template;

    public ValueTemplateItemViewModel()
    {
    }

    public ValueTemplateItemViewModel(
        object? value,
        string? template,
        IList<string>? templateLocations)
    {
        Value = value;
        Template = template;
        if (templateLocations is not null)
        {
            TemplateLocations = new ObservableCollectionEx<string>();
            TemplateLocations.AddRange(templateLocations);
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

    public string GetValueAsString()
        => Value is null
            ? string.Empty
            : Value?.ToString()!;

    public override string ToString()
    {
        if (Template is null || TemplateLocations is null)
        {
            return $"{nameof(Value)}: {Value}";
        }

        return $"{nameof(Value)}: {Value}, {nameof(Template)}: {Template}, {nameof(TemplateLocations)}: {string.Join(" # ", TemplateLocations!)}";
    }
}