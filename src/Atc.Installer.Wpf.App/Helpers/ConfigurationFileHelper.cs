namespace Atc.Installer.Wpf.App.Helpers;

public static class ConfigurationFileHelper
{
    public static bool UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(
        DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);

        var customSettingsFile = new FileInfo(Path.Combine(installationDirectory.FullName, Constants.CustomSettingsFileName));
        var templateSettingsFile = new FileInfo(Path.Combine(installationDirectory.FullName, Constants.TemplateSettingsFileName));
        if (!customSettingsFile.Exists ||
            !templateSettingsFile.Exists)
        {
            return false;
        }

        var customSettings = JsonSerializer.Deserialize<InstallationOption>(
            File.ReadAllText(customSettingsFile.FullName),
            App.JsonSerializerOptions) ?? throw new IOException($"Invalid format in {customSettingsFile.FullName}");

        var templateSettings = JsonSerializer.Deserialize<InstallationOption>(
            File.ReadAllText(templateSettingsFile.FullName),
            App.JsonSerializerOptions) ?? throw new IOException($"Invalid format in {templateSettingsFile.FullName}");

        templateSettings.Azure = customSettings.Azure;
        foreach (var item in customSettings.DefaultApplicationSettings)
        {
            if (templateSettings.DefaultApplicationSettings.ContainsKey(item.Key))
            {
                templateSettings.DefaultApplicationSettings[item.Key] = item.Value;
            }
        }

        var installationSettingsJson = JsonSerializer.Serialize(
            templateSettings,
            App.JsonSerializerOptions);

        var installationSettingsFile = new FileInfo(Path.Combine(installationDirectory.FullName, Constants.InstallationSettingsFileName));
        if (installationSettingsFile.Exists)
        {
            var oldInstallationSettings = File.ReadAllText(installationSettingsFile.FullName);
            if (oldInstallationSettings.Equals(installationSettingsJson, StringComparison.Ordinal))
            {
                return false;
            }

            File.Delete(installationSettingsFile.FullName);
        }

        File.WriteAllText(installationSettingsFile.FullName, installationSettingsJson);

        return true;
    }
}