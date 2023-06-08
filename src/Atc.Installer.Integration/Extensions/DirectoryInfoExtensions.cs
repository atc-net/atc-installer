// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once CheckNamespace
namespace System.IO;

public static class DirectoryInfoExtensions
{
    public static void CopyAll(
        this DirectoryInfo source,
        DirectoryInfo destination,
        bool useRecursive = true,
        bool deleteAllFirst = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (Directory.Exists(destination.FullName))
        {
            if (deleteAllFirst)
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
}