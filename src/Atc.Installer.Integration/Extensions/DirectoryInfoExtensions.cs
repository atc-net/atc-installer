// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable StringLiteralTypo
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once CheckNamespace
namespace System.IO;

public static class DirectoryInfoExtensions
{
    public static void CopyAll(
        this DirectoryInfo source,
        DirectoryInfo destination,
        bool useRecursive = true,
        bool deleteAllFromDestinationBeforeCopy = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (Directory.Exists(destination.FullName))
        {
            if (deleteAllFromDestinationBeforeCopy)
            {
                Directory.Delete(destination.FullName, useRecursive);
                Directory.CreateDirectory(destination.FullName);
            }
        }
        else
        {
            Directory.CreateDirectory(destination.FullName);
        }

        foreach (var sourceFile in Directory.GetFiles(source.FullName))
        {
            var fileName = Path.GetFileName(sourceFile);
            var destinationFile = Path.Combine(destination.FullName, fileName);
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        if (!useRecursive)
        {
            return;
        }

        foreach (var sourceSubDirectory in Directory.GetDirectories(source.FullName))
        {
            var subDirectoryName = Path.GetFileName(sourceSubDirectory);
            var destinationSubDirectory = new DirectoryInfo(Path.Combine(destination.FullName, subDirectoryName));

            CopyAll(
                new DirectoryInfo(sourceSubDirectory),
                destinationSubDirectory,
                useRecursive);
        }
    }

    public static void DeleteAllDirectories(
        this DirectoryInfo directoryInfo,
        string searchPattern = "*",
        bool useRecursive = true,
        IList<string>? excludeDirectories = null)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        var directories = Directory.GetDirectories(directoryInfo.FullName, searchPattern, SearchOption.TopDirectoryOnly);
        foreach (var directory in directories)
        {
            if (excludeDirectories is not null &&
                excludeDirectories.Contains(Path.GetFileName(directory), StringComparer.Ordinal))
            {
                continue;
            }

            Directory.Delete(directory, useRecursive);
        }
    }

    public static void DeleteAllFiles(
        this DirectoryInfo directoryInfo,
        string searchPattern = "*",
        IList<string>? excludeFiles = null)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        var files = Directory.GetFiles(directoryInfo.FullName, searchPattern, SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            if (excludeFiles != null &&
                excludeFiles.Contains(Path.GetFileName(file), StringComparer.Ordinal))
            {
                continue;
            }

            File.Delete(file);
        }
    }

    [SupportedOSPlatform("Windows")]
    public static void SetPermissions(
        this DirectoryInfo directoryInfo,
        string identityReference,
        FileSystemRights fileSystemRights)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        directoryInfo.SetPermissions(
            identityReference,
            fileSystemRights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow);
    }

    [SupportedOSPlatform("Windows")]
    public static void SetPermissions(
        this DirectoryInfo directoryInfo,
        string identityReference,
        FileSystemRights fileSystemRights,
        InheritanceFlags inheritanceFlags,
        PropagationFlags propagationFlags,
        AccessControlType accessControlType)
    {
        ArgumentNullException.ThrowIfNull(directoryInfo);

        if (!Directory.Exists(directoryInfo.FullName))
        {
            Directory.CreateDirectory(directoryInfo.FullName);
            directoryInfo = new DirectoryInfo(directoryInfo.FullName);
        }

        var directorySecurity = directoryInfo.GetAccessControl();

        var fileSystemAccessRule = new FileSystemAccessRule(
            identityReference,
            fileSystemRights,
            inheritanceFlags,
            propagationFlags,
            accessControlType);

        directorySecurity.AddAccessRule(fileSystemAccessRule);

        directoryInfo.SetAccessControl(directorySecurity);
    }
}