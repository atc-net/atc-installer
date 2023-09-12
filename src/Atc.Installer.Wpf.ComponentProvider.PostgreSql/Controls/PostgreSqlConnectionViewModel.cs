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

    public string? GetConnectionString()
    {
        if (string.IsNullOrEmpty(HostName) ||
            HostPort is null ||
            string.IsNullOrEmpty(Database) ||
            string.IsNullOrEmpty(Username) ||
            string.IsNullOrEmpty(Password))
        {
            return null;
        }

        return $"Host={HostName}:{HostPort};Username={Username};Password={Password};Database={Database}";
    }

    public override string ToString()
        => $"{nameof(HostName)}: {HostName}, {nameof(HostPort)}: {HostPort}, {nameof(Database)}: {Database}, {nameof(Username)}: {Username}, {nameof(Password)}: {Password}";
}