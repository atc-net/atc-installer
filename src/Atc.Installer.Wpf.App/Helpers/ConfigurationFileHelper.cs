namespace Atc.Installer.Wpf.App.Helpers;

public static class ConfigurationFileHelper
{
    public static async Task<bool> UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(
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

        var customSettings = await LoadInstallationSettings(customSettingsFile)
            .ConfigureAwait(true);

        var templateSettings = await LoadInstallationSettings(templateSettingsFile)
            .ConfigureAwait(true);

        templateSettings.Azure = customSettings.Azure;
        foreach (var item in customSettings.DefaultApplicationSettings)
        {
            if (templateSettings.DefaultApplicationSettings.ContainsKey(item.Key))
            {
                templateSettings.DefaultApplicationSettings[item.Key] = item.Value;
            }
        }

        var templateSettingsJson = JsonSerializer.Serialize(
            templateSettings,
            App.JsonSerializerOptions);

        var installationSettingsFile = new FileInfo(Path.Combine(installationDirectory.FullName, Constants.InstallationSettingsFileName));
        if (installationSettingsFile.Exists)
        {
            var oldInstallationSettings = await FileHelper
                .ReadAllTextAsync(installationSettingsFile)
                .ConfigureAwait(true);

            if (oldInstallationSettings.Equals(templateSettingsJson, StringComparison.Ordinal))
            {
                return false;
            }

            File.Delete(installationSettingsFile.FullName);
        }

        await SaveInstallationSettings(installationSettingsFile, templateSettingsJson)
            .ConfigureAwait(true);

        return true;
    }

    public static async Task<InstallationOption> LoadInstallationSettings(
        FileInfo installationSettingsFile)
    {
        ArgumentNullException.ThrowIfNull(installationSettingsFile);

        return JsonSerializer.Deserialize<InstallationOption>(
                   await FileHelper
                       .ReadAllTextAsync(installationSettingsFile)
                       .ConfigureAwait(true),
                   App.JsonSerializerOptions) ??
               throw new IOException($"Invalid format in {installationSettingsFile.FullName}");
    }

    public static Task SaveInstallationSettings(
        FileInfo installationSettingsFile,
        InstallationOption installationOption)
    {
        ArgumentNullException.ThrowIfNull(installationSettingsFile);
        ArgumentNullException.ThrowIfNull(installationOption);

        return FileHelper
            .WriteAllTextAsync(
                installationSettingsFile,
                JsonSerializer.Serialize(installationOption, App.JsonSerializerOptions));
    }

    public static Task SaveInstallationSettings(
        FileInfo installationSettingsFile,
        string installationOptionJson)
    {
        ArgumentNullException.ThrowIfNull(installationSettingsFile);
        ArgumentNullException.ThrowIfNull(installationOptionJson);

        return FileHelper
            .WriteAllTextAsync(
                installationSettingsFile,
                installationOptionJson);
    }
}