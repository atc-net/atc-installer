namespace Atc.Installer.Integration.Helpers;

public static class VersionHelper
{
    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
    public static bool IsSourceNewerThanDestination(
        string? sourceVersion,
        string? destinationVersion)
    {
        if (sourceVersion is null ||
            destinationVersion is null ||
            sourceVersion == destinationVersion)
        {
            return false;
        }

        try
        {
            return IsSourceNewerThanDestination(
                new Version(sourceVersion),
                new Version(destinationVersion));
        }
        catch
        {
            var sortedSet = new SortedSet<string>(StringComparer.Ordinal)
            {
                sourceVersion,
                destinationVersion,
            };

            return destinationVersion == sortedSet.First();
        }
    }

    public static bool IsSourceNewerThanDestination(
        Version? sourceVersion,
        Version? destinationVersion)
    {
        if (sourceVersion is null ||
            destinationVersion is null ||
            sourceVersion == destinationVersion)
        {
            return false;
        }

        return sourceVersion.IsNewerThan(destinationVersion);
    }

    public static bool IsDefault(
        Version? sourceVersion,
        Version? destinationVersion)
    {
        if (sourceVersion is null ||
            destinationVersion is null)
        {
            return false;
        }

        return "1.0.0.0".Equals(sourceVersion.ToString(), StringComparison.Ordinal) &&
               "1.0.0.0".Equals(destinationVersion.ToString(), StringComparison.Ordinal);
    }
}