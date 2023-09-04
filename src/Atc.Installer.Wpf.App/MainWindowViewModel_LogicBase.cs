// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class MainWindowViewModel
{
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