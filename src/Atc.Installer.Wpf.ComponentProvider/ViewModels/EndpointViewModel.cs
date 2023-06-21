namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class EndpointViewModel : ViewModelBase
{
    private string name = string.Empty;
    private ComponentEndpointType endpointType;
    private string endpoint = string.Empty;

    public EndpointViewModel(
        string name,
        ComponentEndpointType endpointType,
        string endpoint)
    {
        Name = name;
        EndpointType = endpointType;
        Endpoint = endpoint;
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

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(EndpointType)}: {EndpointType}, {nameof(Endpoint)}: {Endpoint}";
}