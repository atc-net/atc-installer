using Atc.Data;
using Atc.Installer.Integration;
using Atc.Wpf.Command;

namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class ElasticSearchServerComponentProviderViewModel
{
    public IRelayCommandAsync TestConnectionCommand => new RelayCommandAsync(TestConnectionCommandHandler, CanTestConnectionCommandHandler);

    private bool CanTestConnectionCommandHandler()
        => !string.IsNullOrEmpty(ElasticSearchConnectionViewModel.WebProtocol) &&
           !string.IsNullOrEmpty(ElasticSearchConnectionViewModel.HostName) &&
           ElasticSearchConnectionViewModel.HostPort.HasValue &&
           !string.IsNullOrEmpty(ElasticSearchConnectionViewModel.Username) &&
           !string.IsNullOrEmpty(ElasticSearchConnectionViewModel.Password);

    private async Task TestConnectionCommandHandler()
    {
        LogItems.Add(LogItemFactory.CreateTrace("Test connection"));

        var (isSucceeded, errorMessage) = await esInstallerService
            .TestConnection(
                ElasticSearchConnectionViewModel.WebProtocol!,
                ElasticSearchConnectionViewModel.HostName!,
                ElasticSearchConnectionViewModel.HostPort.GetValueOrDefault(),
                ElasticSearchConnectionViewModel.Username!,
                ElasticSearchConnectionViewModel.Password!)
            .ConfigureAwait(true);

        if (isSucceeded)
        {
            LogItems.Add(LogItemFactory.CreateInformation("Test connection succeeded"));
            TestConnectionResult = "Succeeded";
        }
        else
        {
            LogItems.Add(LogItemFactory.CreateError($"Test connection failed: {errorMessage}"));
            TestConnectionResult = "Failed";
        }
    }
}