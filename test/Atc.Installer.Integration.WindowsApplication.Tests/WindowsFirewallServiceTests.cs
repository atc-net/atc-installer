namespace Atc.Installer.Integration.WindowsApplication.Tests;

[SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "OK.")]
[SuppressMessage("Major Code Smell", "S2925:\"Thread.Sleep\" should not be used in tests", Justification = "OK.")]
[SuppressMessage("Microsoft.Interoperability", "CA1416:ValidatePlatformCompatibility", Justification = "OK.")]
[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public class WindowsFirewallServiceTests
{
    [Fact]
    public void AddRemoveInboundRuleForTcpFlow()
    {
        var service = new WindowsFirewallService();

        const string ruleName = "ConsoleApp1";
        const int rulePort = 1234;

        var doesRuleExist1 = service.DoesRuleExist(ruleName);
        Assert.False(doesRuleExist1);

        var addInboundRuleForAllowTcpResult = service.AddInboundRuleForAllowTcp(
            ruleName,
            $"Some description for: {ruleName}",
            rulePort);
        Assert.True(addInboundRuleForAllowTcpResult.IsSucceeded);

        var doesRuleExist2 = service.DoesRuleExist(ruleName);
        Assert.True(doesRuleExist2);

        var isRuleEnabled1 = service.IsRuleEnabled(ruleName);
        Assert.True(isRuleEnabled1);

        var disableRuleResult = service.DisableRule(ruleName);
        Assert.True(disableRuleResult.IsSucceeded);

        var isRuleEnabled2 = service.IsRuleEnabled(ruleName);
        Assert.False(isRuleEnabled2);

        var enableRuleResult = service.EnableRule(ruleName);
        Assert.True(enableRuleResult.IsSucceeded);

        var removeRuleResult = service.RemoveRule(ruleName);
        Assert.True(removeRuleResult.IsSucceeded);

        var doesRuleExist3 = service.DoesRuleExist(ruleName);
        Assert.False(doesRuleExist3);
    }

    [Fact]
    public void AddRemoveInboundRuleForUdpFlow()
    {
        var service = new WindowsFirewallService();

        const string ruleName = "ConsoleApp2";
        const int rulePort = 1235;

        var doesRuleExist1 = service.DoesRuleExist(ruleName);
        Assert.False(doesRuleExist1);

        var addInboundRuleForAllowTcpResult = service.AddInboundRuleForAllowUdp(
            ruleName,
            $"Some description for: {ruleName}",
            rulePort);
        Assert.True(addInboundRuleForAllowTcpResult.IsSucceeded);

        var doesRuleExist2 = service.DoesRuleExist(ruleName);
        Assert.True(doesRuleExist2);

        var isRuleEnabled1 = service.IsRuleEnabled(ruleName);
        Assert.True(isRuleEnabled1);

        var disableRuleResult = service.DisableRule(ruleName);
        Assert.True(disableRuleResult.IsSucceeded);

        var isRuleEnabled2 = service.IsRuleEnabled(ruleName);
        Assert.False(isRuleEnabled2);

        var enableRuleResult = service.EnableRule(ruleName);
        Assert.True(enableRuleResult.IsSucceeded);

        var removeRuleResult = service.RemoveRule(ruleName);
        Assert.True(removeRuleResult.IsSucceeded);

        var doesRuleExist3 = service.DoesRuleExist(ruleName);
        Assert.False(doesRuleExist3);
    }
}