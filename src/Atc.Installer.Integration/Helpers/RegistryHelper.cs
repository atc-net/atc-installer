namespace Atc.Installer.Integration.Helpers;

[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "OK.")]
[SupportedOSPlatform("Windows")]
public static class RegistryHelper
{
    public static (bool IsSucceeded, string? ErrorMessage) CreateSubKeyInLocalMachine(
        string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            using var registryKey = Registry.LocalMachine.CreateSubKey(key);
            if (registryKey is null)
            {
                return (false, $"Registry-LocalMachine-CreateSubKey: {key}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Registry-LocalMachine-CreateSubKey: {key} - {ex.Message}");
        }

        return (true, null);
    }

    public static (bool IsSucceeded, string? ErrorMessage) DeleteSubKeyTreeInLocalMachine(
        string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            return (false, $"Registry-LocalMachine-DeleteSubKeyTree: {key} - {ex.Message}");
        }

        return (true, null);
    }

    public static (bool IsSucceeded, string? ErrorMessage) CreateSubKeyInCurrentUser(
        string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            using var registryKey = Registry.CurrentUser.CreateSubKey(key);
            if (registryKey is null)
            {
                return (false, $"Registry-CurrentUser-CreateSubKey: {key}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Registry-CurrentUser-CreateSubKey: {key} - {ex.Message}");
        }

        return (true, null);
    }

    public static (bool IsSucceeded, string? ErrorMessage) DeleteSubKeyTreeInCurrentUser(
        string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(key, throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            return (false, $"Registry-CurrentUser-DeleteSubKeyTree: {key} - {ex.Message}");
        }

        return (true, null);
    }
}