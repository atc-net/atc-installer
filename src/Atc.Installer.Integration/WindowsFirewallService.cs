namespace Atc.Installer.Integration;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Justification = "OK.")]
[SupportedOSPlatform("Windows")]
public class WindowsFirewallService : IWindowsFirewallService
{
    private readonly INetFwPolicy2? firewallPolicy;

    public WindowsFirewallService()
    {
        var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!;
        firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(policyType)!;
    }

    public bool DoesRuleExist(
        string ruleName)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        return firewallPolicy.Rules
            .Cast<INetFwRule>()
            .Any(x => x.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsRuleEnabled(
        string ruleName)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        if (!DoesRuleExist(ruleName))
        {
            return false;
        }

        var rule = firewallPolicy.Rules
            .Cast<INetFwRule>()
            .First(x => x.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

        return rule.Enabled;
    }

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);

    public (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowAny(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP);

    public (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowAny(
        string ruleName,
        string description,
        int port)
        => AddRule(
            ruleName,
            description,
            port,
            NET_FW_ACTION_.NET_FW_ACTION_ALLOW,
            NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT,
            NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY);

    public (bool IsSucceeded, string? ErrorMessage) EnableRule(
        string ruleName)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        if (!DoesRuleExist(ruleName))
        {
            return (false, "Rule do not exist");
        }

        var rule = firewallPolicy.Rules
            .Cast<INetFwRule>()
            .First(x => x.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

        if (rule.Enabled)
        {
            return (false, "Rule is already enabled");
        }

        try
        {
            rule.Enabled = true;

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool IsSucceeded, string? ErrorMessage) DisableRule(
        string ruleName)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        if (!DoesRuleExist(ruleName))
        {
            return (false, "Rule do not exist");
        }

        var rule = firewallPolicy.Rules
            .Cast<INetFwRule>()
            .First(x => x.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase));

        if (!rule.Enabled)
        {
            return (false, "Rule is already disabled");
        }

        try
        {
            rule.Enabled = false;

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool IsSucceeded, string? ErrorMessage) RemoveRule(
        string ruleName)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        if (!DoesRuleExist(ruleName))
        {
            return (false, "Rule do not exist");
        }

        try
        {
            firewallPolicy.Rules.Remove(ruleName);

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private (bool IsSucceeded, string? ErrorMessage) AddRule(
        string ruleName,
        string description,
        int port,
        NET_FW_ACTION_ action,
        NET_FW_RULE_DIRECTION_ direction,
        NET_FW_IP_PROTOCOL_ protocol)
    {
        if (firewallPolicy is null)
        {
            throw new COMException("FirewallPolicy is not initialized");
        }

        if (DoesRuleExist(ruleName))
        {
            return (false, "Rule already exist");
        }

        try
        {
            var ruleType = Type.GetTypeFromProgID("HNetCfg.FWRule")!;
            var rule = (INetFwRule)Activator.CreateInstance(ruleType)!;

            rule.Name = ruleName;
            rule.Description = description;
            rule.Action = action;
            rule.Direction = direction;
            rule.InterfaceTypes = "All";
            rule.Protocol = (int)protocol;
            rule.Enabled = true;
            rule.LocalPorts = port.ToString(GlobalizationConstants.EnglishCultureInfo);

            firewallPolicy.Rules.Add(rule);

            return (true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Unauthorized Access");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}