// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UseNullPropagation
namespace Atc.Installer.Integration;

/// <summary>
/// InstalledAppsInstallerService
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
/// </remarks>>
[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SuppressMessage("Microsoft.Design", "CA1416:Validate platform compatibility", Justification = "OK.")]
public class InstalledAppsInstallerService : IInstalledAppsInstallerService
{
    private const string InstalledAppsRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string DotNetFrameworkRegistryPath = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
    private const int DonNetFramework480Value = 528040;
    private readonly FileInfo cmdFile = new(@"C:\Windows\System32\cmd.exe");

    public bool IsMicrosoftDotNetFramework48()
        => IsMicrosoftDotNetFramework(DonNetFramework480Value);

    public bool IsMicrosoftDotNet7()
        => IsMicrosoftDotNet(7);

    public bool IsMicrosoftDotNet8()
        => IsMicrosoftDotNet(8);

    public bool IsJavaRuntime8()
        => IsJavaRuntime(8);

    public Task<bool> IsNodeJs18()
        => IsNodeJs(18);

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

    public Version? GetAppInstalledVersionByDisplayName(
        string appDisplayName)
    {
        ArgumentException.ThrowIfNullOrEmpty(appDisplayName);

        if (!IsAppInstalledByDisplayName(appDisplayName))
        {
            return null;
        }

        try
        {
            using var registryKey = Registry.LocalMachine.OpenSubKey(InstalledAppsRegistryPath);
            if (registryKey is null)
            {
                return null;
            }

            foreach (var subKeyName in registryKey.GetSubKeyNames())
            {
                var registrySubKey = registryKey.OpenSubKey(subKeyName);
                if (registrySubKey is null)
                {
                    continue;
                }

                var displayName = (string?)registrySubKey.GetValue("DisplayName");
                if (displayName is null || !displayName.StartsWith(appDisplayName, StringComparison.Ordinal))
                {
                    continue;
                }

                var displayVersion = (string?)registrySubKey.GetValue("DisplayVersion");
                if (displayVersion is not null && displayVersion.Contains('.', StringComparison.Ordinal))
                {
                    return new Version(displayVersion);
                }

                var majorVersion = (int?)registrySubKey.GetValue("MajorVersion");
                var minorVersion = (int?)registrySubKey.GetValue("MinorVersion");
                if (majorVersion.HasValue && minorVersion.HasValue)
                {
                    return new Version(majorVersion.Value, minorVersion.Value);
                }

                return null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsMicrosoftDotNetFramework(
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

    private static bool IsMicrosoftDotNet(
        ushort mainVersion)
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

    private static bool IsJavaRuntime(
        ushort mainVersion)
    {
        try
        {
            var javaFolder = GetJavaFolder();
            if (javaFolder is null)
            {
                return false;
            }

            var filePaths = Directory.GetFiles(javaFolder.FullName, "java.exe", SearchOption.AllDirectories);
            if (!filePaths.Any())
            {
                return false;
            }

            var javaExeFile = new FileInfo(filePaths[0]);
            var parentDirectory = javaExeFile.Directory!.Parent!;
            return parentDirectory.Name.Contains($"1.{mainVersion}", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "OK.")]
    private async Task<bool> IsNodeJs(
        ushort mainVersion)
    {
        var (isSuccessful, output) = await ProcessHelper
            .ExecutePrompt(
                new DirectoryInfo(@"C:\"),
                cmdFile,
                string.Empty,
                new[] { "node.exe -v", "exit" })
            .ConfigureAwait(false);

        if (!isSuccessful || output is null)
        {
            return false;
        }

        var lineWithVersion = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith('v'));

        if (lineWithVersion is null)
        {
            return false;
        }

        var versionAsStr = lineWithVersion[(lineWithVersion.IndexOf('v', StringComparison.Ordinal) + 1)..];
        if (!Version.TryParse(versionAsStr, out var version))
        {
            return false;
        }

        var minVersion = new Version(mainVersion, 0);
        return version.GreaterThanOrEqualTo(minVersion, 1);
    }

    private static DirectoryInfo? GetJavaFolder()
    {
        DirectoryInfo? javaFolder = null;
        if (Environment.Is64BitOperatingSystem)
        {
            var javaFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java");
            if (Directory.Exists(javaFolderPath))
            {
                javaFolder = new DirectoryInfo(javaFolderPath);
            }
            else
            {
                var javaFolderPathX86 =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java");
                if (Directory.Exists(javaFolderPathX86))
                {
                    javaFolder = new DirectoryInfo(javaFolderPathX86);
                }
            }
        }
        else
        {
            var javaFolderPathX86 =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java");
            if (Directory.Exists(javaFolderPathX86))
            {
                javaFolder = new DirectoryInfo(javaFolderPathX86);
            }
        }

        return javaFolder;
    }
}