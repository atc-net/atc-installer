namespace Atc.Installer.Wpf.ComponentProvider.PostgreSql;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class PostgreSqlServerComponentProviderViewModel
{
    public IRelayCommandAsync TestConnectionCommand => new RelayCommandAsync(TestConnectionCommandHandler, CanTestConnectionCommandHandler);

    private bool CanTestConnectionCommandHandler()
        => !string.IsNullOrEmpty(PostgreSqlConnectionViewModel.HostName) &&
           PostgreSqlConnectionViewModel.HostPort.HasValue &&
           !string.IsNullOrEmpty(PostgreSqlConnectionViewModel.Database) &&
           !string.IsNullOrEmpty(PostgreSqlConnectionViewModel.Username) &&
           !string.IsNullOrEmpty(PostgreSqlConnectionViewModel.Password);

    private async Task TestConnectionCommandHandler()
    {
        LogItems.Add(LogItemFactory.CreateTrace("Test connection"));

        var (isSucceeded, errorMessage) = await pgInstallerService
            .TestConnection(
                PostgreSqlConnectionViewModel.HostName!,
                PostgreSqlConnectionViewModel.HostPort.GetValueOrDefault(),
                PostgreSqlConnectionViewModel.Database!,
                PostgreSqlConnectionViewModel.Username!,
                PostgreSqlConnectionViewModel.Password!)
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