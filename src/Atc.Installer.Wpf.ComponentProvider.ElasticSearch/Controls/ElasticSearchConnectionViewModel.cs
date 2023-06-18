namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch.Controls;

public class ElasticSearchConnectionViewModel : ViewModelBase
{
    private string? webProtocol;
    private string? hostName;
    private ushort? hostPort;
    private string? username;
    private string? password;
    private string? index;

    public string? HostName
    {
        get => hostName;
        set
        {
            hostName = value;
            RaisePropertyChanged();
        }
    }

    public string? WebProtocol
    {
        get => webProtocol;
        set
        {
            webProtocol = value;
            RaisePropertyChanged();
        }
    }

    public ushort? HostPort
    {
        get => hostPort;
        set
        {
            hostPort = value;
            RaisePropertyChanged();
        }
    }

    public string? Username
    {
        get => username;
        set
        {
            username = value;
            RaisePropertyChanged();
        }
    }

    public string? Password
    {
        get => password;
        set
        {
            password = value;
            RaisePropertyChanged();
        }
    }

    public string? Index
    {
        get => index;
        set
        {
            index = value;
            RaisePropertyChanged();
        }
    }
}