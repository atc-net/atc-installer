namespace Atc.Installer.Integration.Helpers;

public static class DirectoryHelper
{
    public static bool ExistsAndContainsFiles(
        string? folderPath)
        => folderPath is not null &&
           new DirectoryInfo(folderPath).ExistsAndContainsFiles();

    public static bool ExistsAndContainsNoFiles(
        string? folderPath)
        => folderPath is not null &&
           new DirectoryInfo(folderPath).ExistsAndContainsNoFiles();
}