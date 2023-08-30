namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class FirewallRulesViewModel : ViewModelBase
{
    public FirewallRulesViewModel()
    {
    }

    public FirewallRulesViewModel(
        IList<FirewallRuleOption> firewallRules)
    {
        Populate(firewallRules);
    }

    public ObservableCollectionEx<FirewallRuleViewModel> Items { get; init; } = new();

    public void Populate(
        IList<FirewallRuleOption> firewallRules)
    {
        ArgumentNullException.ThrowIfNull(firewallRules);

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var firewallRule in firewallRules)
        {
            Items.Add(new FirewallRuleViewModel(firewallRule));
        }

        Items.SuppressOnChangedNotification = false;
    }
}