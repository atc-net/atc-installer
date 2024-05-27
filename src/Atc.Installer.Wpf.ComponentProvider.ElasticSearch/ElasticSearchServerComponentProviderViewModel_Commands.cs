namespace Atc.Installer.Wpf.ComponentProvider.ElasticSearch;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class ElasticSearchServerComponentProviderViewModel
{
    public IRelayCommandAsync TestConnectionCommand
        => new RelayCommandAsync(
            TestConnectionCommandHandler,
            CanTestConnectionCommandHandler);

    private bool CanTestConnectionCommandHandler()
        => RunningState == ComponentRunningState.Running &&
           !string.IsNullOrEmpty(ElasticSearchConnection.WebProtocol) &&
           !string.IsNullOrEmpty(ElasticSearchConnection.HostName) &&
           ElasticSearchConnection.HostPort.HasValue &&
           !string.IsNullOrEmpty(ElasticSearchConnection.Username) &&
           !string.IsNullOrEmpty(ElasticSearchConnection.Password);

    private async Task TestConnectionCommandHandler()
    {
        Messenger.Default.Send(new UpdateUserActionTimestampMessage());

        AddLogItem(LogLevel.Trace, "Test connection");

        IsBusy = true;

        var (isSucceeded, errorMessage) = await esInstallerService
            .TestConnection(
                ElasticSearchConnection.WebProtocol!,
                ElasticSearchConnection.HostName!,
                ElasticSearchConnection.HostPort.GetValueOrDefault(),
                ElasticSearchConnection.Username!,
                ElasticSearchConnection.Password!,
                ElasticSearchConnection.Index)
            .ConfigureAwait(true);

        IsBusy = false;

        if (isSucceeded)
        {
            AddLogItem(LogLevel.Information, "Test connection succeeded");
            TestConnectionResult = "Succeeded";
        }
        else
        {
            AddLogItem(LogLevel.Error, $"Test connection failed: {errorMessage}");
            TestConnectionResult = "Failed";
        }
    }
}