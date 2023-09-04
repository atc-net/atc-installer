namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class EndpointViewModel : ViewModelBase
{
    private string name = string.Empty;
    private ComponentEndpointType endpointType;
    private string endpoint = string.Empty;
    private string? template = string.Empty;

    public EndpointViewModel(
        string name,
        ComponentEndpointType endpointType,
        string endpoint,
        string? template,
        IList<string>? templateLocations)
    {
        Name = name;
        EndpointType = endpointType;
        Endpoint = endpoint;
        Template = template;
        if (templateLocations is not null)
        {
            TemplateLocations = new ObservableCollectionEx<string>();
            TemplateLocations.AddRange(templateLocations);
        }
    }

    public string Name
    {
        get => name;
        set
        {
            name = value;
            RaisePropertyChanged();
        }
    }

    public ComponentEndpointType EndpointType
    {
        get => endpointType;
        set
        {
            endpointType = value;
            RaisePropertyChanged();
        }
    }

    public string Endpoint
    {
        get => endpoint;
        set
        {
            endpoint = value;
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

    public ObservableCollectionEx<string>? TemplateLocations { get; }

    public bool ContainsTemplateKey(
        string templateKey)
        => Template is not null &&
           Template.Contains(templateKey, StringComparison.Ordinal);


    public override string ToString()
    {
        if (Template is null || TemplateLocations is null)
        {
            return $"{nameof(Name)}: {Name}, {nameof(EndpointType)}: {EndpointType}, {nameof(Endpoint)}: {Endpoint}";
        }

        return $"{nameof(Name)}: {Name}, {nameof(EndpointType)}: {EndpointType}, {nameof(Endpoint)}: {Endpoint}, {nameof(Template)}: {Template}, {nameof(TemplateLocations)}: {string.Join(" # ", TemplateLocations!)}";
    }
}