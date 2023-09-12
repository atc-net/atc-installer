namespace Atc.Installer.Integration;

public interface IWindowsFirewallService
{
    bool DoesRuleExist(
        string ruleName);

    bool IsRuleEnabled(
        string ruleName);

    (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) AddInboundRuleForAllowAny(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowTcp(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowUdp(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) AddOutboundRuleForAllowAny(
        string ruleName,
        string description,
        int port);

    (bool IsSucceeded, string? ErrorMessage) EnableRule(
        string ruleName);

    (bool IsSucceeded, string? ErrorMessage) DisableRule(
        string ruleName);

    (bool IsSucceeded, string? ErrorMessage) RemoveRule(
        string ruleName);
}