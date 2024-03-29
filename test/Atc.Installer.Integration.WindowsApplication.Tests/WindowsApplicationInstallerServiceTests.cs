// ReSharper disable StringLiteralTypo
namespace Atc.Installer.Integration.WindowsApplication.Tests;

[SuppressMessage("Major Code Smell", "S2925:\"Thread.Sleep\" should not be used in tests", Justification = "OK.")]
[SuppressMessage("Microsoft.Interoperability", "CA1416:ValidatePlatformCompatibility", Justification = "OK.")]
[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public class WindowsApplicationInstallerServiceTests
{
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