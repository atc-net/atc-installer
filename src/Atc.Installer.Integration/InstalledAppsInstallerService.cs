// ReSharper disable LoopCanBeConvertedToQuery
namespace Atc.Installer.Integration;

/// <summary>
/// InstalledAppsInstallerService
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
/// </remarks>>
[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Design", "CA1416:Validate platform compatibility", Justification = "OK.")]
[SuppressMessage("Design", "MA0076:Do not use implicit culture-sensitive ToString in interpolated strings", Justification = "OK.")]
public class InstalledAppsInstallerService : IInstalledAppsInstallerService
{
    private const string InstalledAppsRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string DotNetFrameworkRegistryPath = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
    private const int DonNetFramework480Value = 528040;
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

    public bool IsMicrosoftDonNetFramework48()
        => IsMicrosoftDonNetFramework(DonNetFramework480Value);

    public bool IsMicrosoftDonNet7()
        => IsMicrosoftDonNet(7);

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

    private static bool IsMicrosoftDonNetFramework(
        int versionValue)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            var subKey = baseKey.OpenSubKey(DotNetFrameworkRegistryPath);
            return subKey?.GetValue("Release") is not null &&
                   (int)(subKey.GetValue("Release") ?? 0) >= versionValue;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsMicrosoftDonNet(
        int mainVersion)
    {
        try
        {
            var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            if (runtimeDirectory is null)
            {
                return false;
            }

            var directories = Directory
                .EnumerateFileSystemEntries(runtimeDirectory)
                .ToArray();

            return directories.Any(x => x.Contains($"Microsoft.NETCore.App\\{mainVersion}.", StringComparison.Ordinal));
        }
        catch
        {
            return false;
        }
    }
}