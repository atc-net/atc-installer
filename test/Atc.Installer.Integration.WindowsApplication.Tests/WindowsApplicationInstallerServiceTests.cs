namespace Atc.Installer.Integration.WindowsApplication.Tests;

[SuppressMessage("Microsoft.Interoperability", "CA1416:ValidatePlatformCompatibility", Justification = "OK.")]
public class WindowsApplicationInstallerServiceTests
{
    [Fact]
    public void StopAndStartApplicationFlow_ApplicationName()
    {
        var iaInstallerService = new InstalledAppsInstallerService();
        var sut = new WindowsApplicationInstallerService(iaInstallerService);

        var applicationName = "notepad";

        var runningState = sut.GetApplicationState(applicationName);
        Assert.Equal(ComponentRunningState.NotAvailable, runningState);

        var isStarted = sut.StartApplication(applicationName);
        Assert.True(isStarted);

        Thread.Sleep(1_000);

        runningState = sut.GetApplicationState(applicationName);
        Assert.Equal(ComponentRunningState.Running, runningState);

        var isStopped = sut.StopApplication(applicationName);
        Assert.True(isStopped);
    }

    [Fact]
    public void StopAndStartApplicationFlow_ApplicationFile()
    {
        var iaInstallerService = new InstalledAppsInstallerService();
        var sut = new WindowsApplicationInstallerService(iaInstallerService);

        var applicationFile = new FileInfo(@"C:\Windows\notepad.exe");

        var runningState = sut.GetApplicationState(applicationFile);
        Assert.Equal(ComponentRunningState.NotAvailable, runningState);

        var isStarted = sut.StartApplication(applicationFile);
        Assert.True(isStarted);

        Thread.Sleep(1_000);

        runningState = sut.GetApplicationState(applicationFile);
        Assert.Equal(ComponentRunningState.Running, runningState);

        var isStopped = sut.StopApplication(applicationFile);
        Assert.True(isStopped);
    }
}