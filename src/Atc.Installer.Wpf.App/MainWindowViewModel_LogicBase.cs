// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable UnusedVariable
namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class MainWindowViewModel
{
    private void LoadRecentOpenFiles()
    {
        var recentOpenFilesFile = Path.Combine(App.InstallerProgramDataDirectory.FullName, Constants.RecentOpenFilesFileName);
        if (!File.Exists(recentOpenFilesFile))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(recentOpenFilesFile);

            var recentOpenFilesOption = JsonSerializer.Deserialize<RecentOpenFilesOption>(
                json,
                App.JsonSerializerOptions) ?? throw new IOException($"Invalid format in {recentOpenFilesFile}");

            RecentOpenFiles.Clear();

            RecentOpenFiles.SuppressOnChangedNotification = true;
            foreach (var recentOpenFile in recentOpenFilesOption.RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
            {
                if (!File.Exists(recentOpenFile.FilePath))
                {
                    continue;
                }

                RecentOpenFiles.Add(new RecentOpenFileViewModel(App.InstallerProgramDataProjectsDirectory, recentOpenFile.TimeStamp, recentOpenFile.FilePath));
            }

            RecentOpenFiles.SuppressOnChangedNotification = false;
        }
        catch
        {
            // Skip
        }
    }

    private void AddLoadedFileToRecentOpenFiles(
        FileInfo file)
    {
        RecentOpenFiles.Add(new RecentOpenFileViewModel(App.InstallerProgramDataProjectsDirectory, DateTime.Now, file.FullName));

        var recentOpenFilesOption = new RecentOpenFilesOption();
        foreach (var vm in RecentOpenFiles.OrderByDescending(x => x.TimeStamp))
        {
            var item = new RecentOpenFileOption
            {
                TimeStamp = vm.TimeStamp,
                FilePath = vm.File,
            };

            if (recentOpenFilesOption.RecentOpenFiles.FirstOrDefault(x => x.FilePath == item.FilePath) is not null)
            {
                continue;
            }

            if (!File.Exists(item.FilePath))
            {
                continue;
            }

            recentOpenFilesOption.RecentOpenFiles.Add(item);
        }

        var recentOpenFilesFilePath = Path.Combine(App.InstallerProgramDataDirectory.FullName, Constants.RecentOpenFilesFileName);
        if (!Directory.Exists(App.InstallerProgramDataDirectory.FullName))
        {
            Directory.CreateDirectory(App.InstallerProgramDataDirectory.FullName);
        }

        var json = JsonSerializer.Serialize(recentOpenFilesOption, App.JsonSerializerOptions);
        File.WriteAllText(recentOpenFilesFilePath, json);

        LoadRecentOpenFiles();
    }

    private static void ValidateConfigurationFile(
        InstallationOption installationOptions)
    {
        var errors = new List<string>();
        foreach (var applicationOption in installationOptions.Applications)
        {
            if (string.IsNullOrEmpty(applicationOption.Name))
            {
                errors.Add($"{nameof(applicationOption.Name)} is missing");
            }

            if (string.IsNullOrEmpty(applicationOption.RawInstallationPath) &&
                string.IsNullOrEmpty(applicationOption.InstallationFile) &&
                applicationOption.HostingFramework != HostingFrameworkType.NativeNoSettings)
            {
                errors.Add($"{applicationOption.Name}->{nameof(applicationOption.InstallationFile)} is invalid");
            }

            if (string.IsNullOrEmpty(applicationOption.InstallationPath))
            {
                errors.Add($"{applicationOption.Name}->{nameof(applicationOption.InstallationPath)} is invalid");
            }
        }

        if (errors.Any())
        {
            throw new ValidationException(string.Join(Environment.NewLine, errors));
        }
    }

    private async Task LoadConfigurationFile(
        FileInfo file,
        CancellationToken cancellationToken)
    {
        try
        {
            loggerComponentProvider.Log(LogLevel.Trace, $"Loading configuration file: {file.FullName}");

            StopMonitoringServices();

            var json = await File
                .ReadAllTextAsync(file.FullName, cancellationToken)
                .ConfigureAwait(true);

            var installationOptions = JsonSerializer.Deserialize<InstallationOption>(
                json,
                App.JsonSerializerOptions) ?? throw new IOException($"Invalid format in {file}");

            installationDirectory = new DirectoryInfo(file.Directory!.FullName);

            ValidateConfigurationFile(installationOptions);

            Populate(file, installationOptions);

            AddLoadedFileToRecentOpenFiles(file);

            StartMonitoringServices();

            loggerComponentProvider.Log(LogLevel.Trace, $"Loaded configuration file: {file.FullName}");
        }
        catch (Exception ex)
        {
            loggerComponentProvider.Log(LogLevel.Error, $"Configuration file: {file.FullName}, Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private async Task SaveConfigurationFile()
    {
        try
        {
            loggerComponentProvider.Log(LogLevel.Trace, $"Saving configuration file: {InstallationFile!.FullName}");

            var installationOption = new InstallationOption();
            if (ProjectName is not null)
            {
                installationOption.Name = ProjectName;
            }

            if (AzureOptions is not null)
            {
                installationOption.Azure = new AzureOptions
                {
                    StorageConnectionString = AzureOptions.StorageConnectionString,
                    BlobContainerName = AzureOptions.BlobContainerName,
                };
            }

            foreach (var keyValueTemplateItem in DefaultApplicationSettings)
            {
                installationOption.DefaultApplicationSettings.Add(
                    new KeyValuePair<string, object>(
                        keyValueTemplateItem.Key,
                        keyValueTemplateItem.Value));
            }

            foreach (var componentProvider in ComponentProviders)
            {
                var applicationOption = CreateApplicationOption(componentProvider);

                installationOption.Applications.Add(applicationOption);
            }

            await ConfigurationFileHelper
                .SaveInstallationSettings(InstallationFile, installationOption)
                .ConfigureAwait(true);

            loggerComponentProvider.Log(LogLevel.Trace, $"Saving configuration file: {InstallationFile!.FullName}");

            IsDirty = false;
            foreach (var componentProvider in ComponentProviders)
            {
                componentProvider.ClearAllIsDirty();
            }

            var customSettingsFile = new FileInfo(Path.Combine(InstallationFile.Directory!.FullName, Constants.CustomSettingsFileName));
            var templateSettingsFile = new FileInfo(Path.Combine(InstallationFile.Directory!.FullName, Constants.TemplateSettingsFileName));
            if (customSettingsFile.Exists &&
                templateSettingsFile.Exists)
            {
                var json = JsonSerializer.Serialize(installationOption, App.JsonSerializerOptions);
                var dynamicJsonCustomSettings = new DynamicJson(json);
                dynamicJsonCustomSettings.RemovePath("Applications");
                await ConfigurationFileHelper.SaveInstallationSettings(
                        customSettingsFile,
                        dynamicJsonCustomSettings.ToJson())
                    .ConfigureAwait(true);

                installationOption.ClearDataForTemplateSettings();
                var jsonTemplateSettings = JsonSerializer.Serialize(installationOption, App.JsonSerializerOptions);
                await ConfigurationFileHelper.SaveInstallationSettings(
                        templateSettingsFile,
                        jsonTemplateSettings)
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            loggerComponentProvider.Log(LogLevel.Error, $"Configuration file: {InstallationFile!.FullName}, Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private static string? GetTemplateSettingsWithInstallationFileContentHash(
        DirectoryInfo installationDirectory)
    {
        var templateSettingsFile = new FileInfo(Path.Combine(installationDirectory.FullName, Constants.TemplateSettingsFileName));
        return templateSettingsFile.Exists
            ? CalculateMd5(templateSettingsFile)
            : null;
    }

    private List<(string ComponentName, string? ContentHash)> GetComponentsWithInstallationFileContentHash()
    {
        var components = new List<(string ComponentName, string? ContentHash)>();
        foreach (var vm in ComponentProviders)
        {
            if (vm.InstallationFile is null)
            {
                components.Add((vm.Name, ContentHash: null));
            }
            else
            {
                var existingInstallationFileInfo = new FileInfo(Path.Combine(vm.InstallationDirectory.FullName, vm.InstallationFile));
                if (existingInstallationFileInfo.Exists)
                {
                    var calculateMd5 = CalculateMd5(existingInstallationFileInfo);
                    components.Add((vm.Name, ContentHash: calculateMd5));
                }
                else
                {
                    components.Add((vm.Name, ContentHash: null));
                }
            }
        }

        return components;
    }

    private static string CalculateMd5(
        FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(file.FullName);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.Ordinal);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK.")]
    [SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "OK.")]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "OK.")]
    private static ApplicationOption CreateApplicationOption(
        ComponentProviderViewModel componentProvider)
    {
        var applicationOption = new ApplicationOption
        {
            Name = componentProvider.Name,
            ServiceName = componentProvider.ServiceName,
            ComponentType = componentProvider.ComponentType,
            HostingFramework = componentProvider.HostingFramework,
            InstallationFile = componentProvider.InstallationFile,
            InstallationPath = componentProvider.InstallationFolderPath?.Template is null
                ? componentProvider.InstallationFolderPath?.GetValueAsString() ?? string.Empty
                : componentProvider.InstallationFolderPath?.Template ?? string.Empty,
        };

        switch (componentProvider)
        {
            case WindowsApplicationComponentProviderViewModel windowsApplicationComponentProviderViewModel:
                applicationOption.DependentComponents = windowsApplicationComponentProviderViewModel.DependentComponents;
                break;
            case ElasticSearchServerComponentProviderViewModel elasticSearchServerComponentProviderViewModel:
                if (elasticSearchServerComponentProviderViewModel.IsRequiredJava)
                {
                    applicationOption.DependentComponents.Add("Java");
                }

                break;
            case InternetInformationServerComponentProviderViewModel informationServerComponentProviderViewModel:
                if (informationServerComponentProviderViewModel.IsRequiredWebSockets)
                {
                    applicationOption.DependentComponents.Add("WebSockets");
                }

                break;
            case PostgreSqlServerComponentProviderViewModel postgreSqlServerComponentProviderViewModel:
                break;
        }

        foreach (var dependentService in componentProvider.DependentServices)
        {
            applicationOption.DependentServices.Add(dependentService.Name);
        }

        foreach (var keyValueTemplateItem in componentProvider.ApplicationSettings.Items)
        {
            applicationOption.ApplicationSettings.Add(
                keyValueTemplateItem.Key,
                keyValueTemplateItem.Template ?? keyValueTemplateItem.Value);
        }

        foreach (var folderPermission in componentProvider.FolderPermissions.Items)
        {
            applicationOption.FolderPermissions.Add(
                new FolderPermissionOption
                {
                    User = folderPermission.User,
                    FileSystemRights = folderPermission.FileSystemRights.ToString(),
                    Folder = folderPermission.Folder,
                });
        }

        foreach (var firewallRule in componentProvider.FirewallRules.Items)
        {
            applicationOption.FirewallRules.Add(
                new FirewallRuleOption
                {
                    Name = firewallRule.Name,
                    Port = firewallRule.Port,
                    Direction = firewallRule.Direction,
                    Protocol = firewallRule.Protocol,
                });
        }

        foreach (var jsonItem in componentProvider.ConfigurationSettingsFiles.JsonItems)
        {
            var option = new ConfigurationSettingsFileOption
            {
                FileName = jsonItem.FileName,
            };

            foreach (var keyValueTemplateItem in jsonItem.Settings)
            {
                option.JsonSettings.Add(
                    keyValueTemplateItem.Key,
                    keyValueTemplateItem.Template ?? keyValueTemplateItem.Value);
            }

            applicationOption.ConfigurationSettingsFiles.Add(option);
        }

        foreach (var xmlItem in componentProvider.ConfigurationSettingsFiles.XmlItems)
        {
            var option = new ConfigurationSettingsFileOption
            {
                FileName = xmlItem.FileName,
            };

            foreach (var xmlElementItem in xmlItem.Settings)
            {
                var xmlElementSettingsOptions = new XmlElementSettingsOptions
                {
                    Element = xmlElementItem.Element,
                    Path = xmlElementItem.Path,
                };

                foreach (var keyValueTemplateItem in xmlElementItem.Attributes)
                {
                    xmlElementSettingsOptions.Attributes.Add(
                        keyValueTemplateItem.Key,
                        keyValueTemplateItem.Template ?? keyValueTemplateItem.GetValueAsString());
                }

                option.XmlSettings.Add(xmlElementSettingsOptions);
            }

            applicationOption.ConfigurationSettingsFiles.Add(option);
        }

        foreach (var endpoint in componentProvider.Endpoints)
        {
            if (endpoint.EndpointType == ComponentEndpointType.BrowserLink &&
                endpoint.Name.StartsWith("Http", StringComparison.Ordinal))
            {
                continue;
            }

            applicationOption.Endpoints.Add(
                new EndpointOption
                {
                    Name = endpoint.Name,
                    EndpointType = endpoint.EndpointType,
                    Endpoint = endpoint.Template ?? endpoint.Endpoint,
                });
        }

        return applicationOption;
    }
}