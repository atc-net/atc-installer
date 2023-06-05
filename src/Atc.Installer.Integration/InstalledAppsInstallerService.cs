// ReSharper disable LoopCanBeConvertedToQuery
namespace Atc.Installer.Integration;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Design", "CA1416:Validate platform compatibility", Justification = "OK.")]
public class InstalledAppsInstallerService : IInstalledAppsInstallerService
{
    private const string InstalledAppsRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private static readonly object InstanceLock = new();
    private static InstalledAppsInstallerService? instance;

    private InstalledAppsInstallerService()
    {
    }

    public static InstalledAppsInstallerService Instance
    {
        get
        {
            lock (InstanceLock)
            {
                return instance ??= new InstalledAppsInstallerService();
            }
        }
    }

    public bool IsAppInstalledByDisplayName(
        string appDisplayName)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDisplayName);

        try
        {
            using var registryKey = Registry.LocalMachine.OpenSubKey(InstalledAppsRegistryPath);
            if (registryKey is null)
            {
                return false;
            }

            foreach (var subKeyName in registryKey.GetSubKeyNames())
            {
                var registrySubKey = registryKey.OpenSubKey(subKeyName);
                var displayName = (string?)registrySubKey?.GetValue("DisplayName");
                if (displayName is null)
                {
                    continue;
                }

                if (displayName.StartsWith(appDisplayName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}