namespace Atc.Installer.Integration.InstallationConfigurations;

public class FirewallRuleOption
{
    public string Name { get; set; } = string.Empty;

    public int Port { get; set; }

    public FirewallProtocolType Protocol { get; set; } = FirewallProtocolType.Tcp;

    public FirewallDirectionType Direction { get; set; } = FirewallDirectionType.Inbound;

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(Port)}: {Port}, {nameof(Protocol)}: {Protocol}, {nameof(Direction)}: {Direction}";
}