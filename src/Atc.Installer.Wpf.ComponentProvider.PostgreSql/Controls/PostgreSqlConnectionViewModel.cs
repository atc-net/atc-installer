namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql.Controls;

public class PostgreSqlConnectionViewModel : ViewModelBase
{
    private string? hostName;
    private ushort? hostPort;
    private string? database;
    private string? username;
    private string? password;

    public string? HostName
    {
        get => hostName;
        set
        {
            hostName = value;
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

    public string? Database
    {
        get => database;
        set
        {
            database = value;
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
}