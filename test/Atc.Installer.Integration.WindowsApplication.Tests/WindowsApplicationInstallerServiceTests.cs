namespace Atc.Installer.Integration.WindowsApplication.Tests;

public class WindowsApplicationInstallerServiceTests
{
    [Fact]
    public void StopAndStartApplicationFlow()
    {
        var applicationName = "notepad";
        var wsInstallerService = WindowsApplicationInstallerService.Instance;

        var runningState = wsInstallerService.GetApplicationState(applicationName);
        Assert.Equal(ComponentRunningState.NotAvailable, runningState);

        var isStarted = wsInstallerService.StartApplication(applicationName);
        Assert.True(isStarted);

        Thread.Sleep(1_000);

        runningState = wsInstallerService.GetApplicationState(applicationName);
        Assert.Equal(ComponentRunningState.Running, runningState);

        var isStopped = wsInstallerService.StopApplication(applicationName);
        Assert.True(isStopped);
    }
}