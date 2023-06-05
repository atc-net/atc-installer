namespace Atc.Installer.Integration.WindowsApplication;

public sealed class WindowsApplicationInstallerService : IWindowsApplicationInstallerService
{
    private static readonly object InstanceLock = new();
    private static WindowsApplicationInstallerService? instance;

    private WindowsApplicationInstallerService()
    {
    }

    public static WindowsApplicationInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new WindowsApplicationInstallerService();
            }
        }
    }
}