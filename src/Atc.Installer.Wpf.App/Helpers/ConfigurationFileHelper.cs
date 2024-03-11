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

        MapCustomSettingsToTemplateSettings(templateSettings, customSettings);

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

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    private static void MapCustomSettingsToTemplateSettings(
        InstallationOption templateSettings,
        InstallationOption customSettings)
    {
        // For Azure and DefaultApplicationSettings - overwrite all
        templateSettings.Azure = customSettings.Azure;
        foreach (var customDefaultApplicationSetting in customSettings.DefaultApplicationSettings)
        {
            if (templateSettings.DefaultApplicationSettings.ContainsKey(customDefaultApplicationSetting.Key))
            {
                templateSettings.DefaultApplicationSettings[customDefaultApplicationSetting.Key] =
                    customDefaultApplicationSetting.Value;
            }
        }

        // For all under Applications, add only missing items to templateSettings
        foreach (var customApplication in customSettings.Applications)
        {
            var templateApplication = templateSettings.Applications.FirstOrDefault(x => x.Name == customApplication.Name);
            if (templateApplication is null)
            {
                continue;
            }

            foreach (var customApplicationSetting in customApplication.ApplicationSettings)
            {
                if (templateApplication.ApplicationSettings.FirstOrDefault(x => x.Key == customApplicationSetting.Key)
                        .Key is null)
                {
                    templateApplication.ApplicationSettings.Add(customApplicationSetting);
                }
            }

            foreach (var customFolderPermission in customApplication.FolderPermissions)
            {
                if (templateApplication.FolderPermissions.FirstOrDefault(x => x.Folder == customFolderPermission.Folder) is
                    null)
                {
                    templateApplication.FolderPermissions.Add(customFolderPermission);
                }
            }

            foreach (var customRegistrySetting in customApplication.RegistrySettings)
            {
                if (templateApplication.RegistrySettings.FirstOrDefault(x => x.Key == customRegistrySetting.Key) is
                    null)
                {
                    templateApplication.RegistrySettings.Add(customRegistrySetting);
                }
            }

            foreach (var customFirewallRule in customApplication.FirewallRules)
            {
                if (templateApplication.FirewallRules.FirstOrDefault(x => x.Name == customFirewallRule.Name) is null)
                {
                    templateApplication.FirewallRules.Add(customFirewallRule);
                }
            }

            foreach (var customConfigurationSettingsFile in customApplication.ConfigurationSettingsFiles)
            {
                var templateConfigurationSettingsFile =
                    templateApplication.ConfigurationSettingsFiles.FirstOrDefault(x =>
                        x.FileName == customConfigurationSettingsFile.FileName);
                if (templateConfigurationSettingsFile is null)
                {
                    continue;
                }

                foreach (var customJsonSetting in customConfigurationSettingsFile.JsonSettings)
                {
                    if (templateConfigurationSettingsFile.JsonSettings.FirstOrDefault(x => x.Key == customJsonSetting.Key)
                            .Key is null)
                    {
                        templateConfigurationSettingsFile.JsonSettings.Add(customJsonSetting);
                    }
                }

                foreach (var customXmlSetting in customConfigurationSettingsFile.XmlSettings)
                {
                    foreach (var customAttribute in customXmlSetting.Attributes)
                    {
                        var isFound = templateConfigurationSettingsFile.XmlSettings
                            .Any(x => x.Attributes.Values.Contains(customAttribute.Value, StringComparer.Ordinal));

                        if (!isFound)
                        {
                            templateConfigurationSettingsFile.XmlSettings.Add(customXmlSetting);
                        }
                    }
                }
            }
        }
    }
}