namespace Atc.Installer.Wpf.ComponentProvider;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1502:Element should not be on a single line", Justification = "OK - ByDesign.")]
public partial class ComponentProviderViewModel
{
    public IRelayCommandAsync ServiceStopCommand
        => new RelayCommandAsync(
            ServiceStopCommandHandler,
            CanServiceStopCommandHandler);

    public IRelayCommandAsync ServiceStartCommand
        => new RelayCommandAsync(
            ServiceStartCommandHandler,
            CanServiceStartCommandHandler);

    public IRelayCommandAsync ServiceDeployCommand
        => new RelayCommandAsync(
            ServiceDeployCommandHandler,
            CanServiceDeployCommandHandler);

    public IRelayCommandAsync ServiceDeployAndStartCommand
        => new RelayCommandAsync(
            ServiceDeployAndStartCommandHandler,
            CanServiceDeployCommandHandler);

    public IRelayCommand<string> ServiceEndpointBrowserLinkCommand
        => new RelayCommand<string>(
            ServiceEndpointBrowserLinkCommandHandler,
            CanServiceEndpointBrowserLinkCommandHandler);

    public virtual bool CanServiceStopCommandHandler()
        => false;

    public virtual Task ServiceStopCommandHandler()
        => Task.CompletedTask;

    public virtual bool CanServiceStartCommandHandler()
        => false;

    public virtual Task ServiceStartCommandHandler()
        => Task.CompletedTask;

    public virtual bool CanServiceDeployCommandHandler()
        => false;

    public virtual Task ServiceDeployCommandHandler()
        => Task.CompletedTask;

    public virtual Task ServiceDeployAndStartCommandHandler()
        => Task.CompletedTask;

    private bool CanServiceEndpointBrowserLinkCommandHandler(
        string endpoint)
        => RunningState == ComponentRunningState.Running;

    private static void ServiceEndpointBrowserLinkCommandHandler(
        string endpoint)
        => InternetBrowserHelper.OpenUrl(new Uri(endpoint));
}