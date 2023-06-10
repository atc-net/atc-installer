namespace Atc.Installer.Integration.WindowsApplication.Tests;

public class WindowsApplicationInstallerServiceTests
{
    [Fact]
    public void StopAndStartApplicationFlow_ApplicationName()
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

    [Fact]
    public void StopAndStartApplicationFlow_ApplicationFile()
    {
        var applicationFile = new FileInfo(@"C:\Windows\notepad.exe");
        var wsInstallerService = WindowsApplicationInstallerService.Instance;

        var runningState = wsInstallerService.GetApplicationState(applicationFile);
        Assert.Equal(ComponentRunningState.NotAvailable, runningState);

        var isStarted = wsInstallerService.StartApplication(applicationFile);
        Assert.True(isStarted);

        Thread.Sleep(1_000);

        runningState = wsInstallerService.GetApplicationState(applicationFile);
        Assert.Equal(ComponentRunningState.Running, runningState);

        var isStopped = wsInstallerService.StopApplication(applicationFile);
        Assert.True(isStopped);
    }
}