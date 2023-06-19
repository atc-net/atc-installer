namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class PostgreSqlServerComponentProviderViewModel
{
    public IRelayCommandAsync TestConnectionCommand => new RelayCommandAsync(TestConnectionCommandHandler, CanTestConnectionCommandHandler);

    private bool CanTestConnectionCommandHandler()
        => RunningState == ComponentRunningState.Running &&
           !string.IsNullOrEmpty(PostgreSqlConnection.HostName) &&
           PostgreSqlConnection.HostPort.HasValue &&
           !string.IsNullOrEmpty(PostgreSqlConnection.Database) &&
           !string.IsNullOrEmpty(PostgreSqlConnection.Username) &&
           !string.IsNullOrEmpty(PostgreSqlConnection.Password);

    private async Task TestConnectionCommandHandler()
    {
        LogItems.Add(LogItemFactory.CreateTrace("Test connection"));

        var (isSucceeded, errorMessage) = await pgInstallerService
            .TestConnection(
                PostgreSqlConnection.HostName!,
                PostgreSqlConnection.HostPort.GetValueOrDefault(),
                PostgreSqlConnection.Database!,
                PostgreSqlConnection.Username!,
                PostgreSqlConnection.Password!)
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