namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class FirewallRuleViewModel : ViewModelBase
{
    private string name = string.Empty;
    private int port;
    private FirewallDirectionType direction = FirewallDirectionType.Inbound;
    private FirewallProtocolType protocol = FirewallProtocolType.Tcp;

    public FirewallRuleViewModel()
    {
    }

    public FirewallRuleViewModel(
        FirewallRuleOption firewallRule)
    {
        ArgumentNullException.ThrowIfNull(firewallRule);

        Name = firewallRule.Name;
        Port = firewallRule.Port;
        Direction = firewallRule.Direction;
        Protocol = firewallRule.Protocol;
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

    public int Port
    {
        get => port;
        set
        {
            port = value;
            RaisePropertyChanged();
        }
    }

    public FirewallDirectionType Direction
    {
        get => direction;
        set
        {
            direction = value;
            RaisePropertyChanged();
        }
    }

    public FirewallProtocolType Protocol
    {
        get => protocol;
        set
        {
            protocol = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(Port)}: {Port}, {nameof(Direction)}: {Direction}, {nameof(Protocol)}: {Protocol}";
}