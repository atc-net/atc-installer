// ReSharper disable LoopCanBeConvertedToQuery
namespace Atc.Installer.Wpf.App;

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - partial class")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "OK - partial class")]
[SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "OK.")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "OK.")]
public partial class MainWindowViewModel
{
    public IRelayCommandAsync OpenConfigurationFileCommand
        => new RelayCommandAsync(
            OpenConfigurationFileCommandHandler);

    public IRelayCommand OpenApplicationSettingsCommand
        => new RelayCommand(
            OpenApplicationSettingsCommandHandler);

    public IRelayCommandAsync<string> OpenRecentConfigurationFileCommand
        => new RelayCommandAsync<string>(
            OpenRecentConfigurationFileCommandHandler);

    public IRelayCommandAsync SaveConfigurationFileCommand
        => new RelayCommandAsync(
            SaveConfigurationFileCommandHandler,
            CanSaveConfigurationFileCommandHandler);

    public IRelayCommandAsync DownloadInstallationFilesFromAzureStorageAccountCommand
        => new RelayCommandAsync(
            DownloadInstallationFilesFromAzureStorageAccountCommandHandler,
            CanDownloadInstallationFilesFromAzureStorageAccountCommandHandler);

    public IRelayCommand OpenApplicationCheckForUpdatesCommand
        => new RelayCommand(
            OpenApplicationCheckForUpdatesCommandHandler,
            CanOpenApplicationCheckForUpdatesCommandHandler);

    public static IRelayCommand OpenApplicationAboutCommand
        => new RelayCommand(
            OpenApplicationAboutCommandHandler);

    public IRelayCommandAsync ServiceStopAllCommand
        => new RelayCommandAsync(
            ServiceStopAllCommandHandler,
            CanServiceStopAllCommandHandler);

    public IRelayCommandAsync ServiceDeployAllCommand
        => new RelayCommandAsync(
            ServiceDeployAllCommandHandler,
            CanServiceDeployAllCommandHandler);

    public IRelayCommandAsync ServiceStartAllCommand
        => new RelayCommandAsync(
            ServiceStartAllCommandHandler,
            CanServiceStartAllCommandHandler);

    private async Task OpenConfigurationFileCommandHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            InitialDirectory = App.InstallerProgramDataProjectsDirectory.FullName,
            Multiselect = false,
            Filter = "Configuration Files|*InstallationSettings.json",
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        await LoadConfigurationFile(
            new FileInfo(openFileDialog.FileName),
            CancellationToken.None).ConfigureAwait(true);
    }

    private void OpenApplicationSettingsCommandHandler()
    {
        var vm = new ApplicationSettingsDialogViewModel(ApplicationOptions);
        var dialogResult = new ApplicationSettingsDialog(vm).ShowDialog();
        if (!dialogResult.HasValue)
        {
            return;
        }

        if (dialogResult.Value)
        {
            ApplicationOptions = vm.ApplicationOptions.Clone();
        }
    }

    private bool CanOpenRecentConfigurationFileCommandHandler()
        => RecentOpenFiles is not null &&
           RecentOpenFiles.Count != 0;

    private Task OpenRecentConfigurationFileCommandHandler(
        string filePath)
        => LoadConfigurationFile(
            new FileInfo(filePath),
            CancellationToken.None);

    private bool CanSaveConfigurationFileCommandHandler()
    {
        return ApplicationOptions.EnableEditingMode &&
               InstallationFile is not null &&
               (IsDirty ||
                ComponentProviders.Any(x => x.IsDirty ||
                                            x.DefaultApplicationSettings.IsDirty ||
                                            x.ApplicationSettings.IsDirty ||
                                            x.FolderPermissions.IsDirty ||
                                            x.FirewallRules.IsDirty ||
                                            x.ConfigurationSettingsFiles.IsDirty));
    }

    private async Task SaveConfigurationFileCommandHandler()
    {
        // TODO: Use InstallationFile
        var file = new FileInfo(@"C:\Temp\test.json");
        try
        {
            loggerComponentProvider.Log(LogLevel.Trace, $"Saving configuration file: {file.FullName}");

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
                    new KeyValuePair<string, object>(keyValueTemplateItem.Key, keyValueTemplateItem.Value));
            }

            foreach (var componentProvider in ComponentProviders)
            {
                var applicationOption = CreateApplicationOption(componentProvider);

                installationOption.Applications.Add(applicationOption);
            }

            var json = JsonSerializer.Serialize(installationOption, App.JsonSerializerOptions);
            await FileHelper.WriteAllTextAsync(file, json, cancellationTokenSource!.Token).ConfigureAwait(true);

            loggerComponentProvider.Log(LogLevel.Trace, $"Saving configuration file: {file.FullName}");

            IsDirty = false;
            foreach (var componentProvider in ComponentProviders)
            {
                componentProvider.ClearAllIsDirty();
            }
        }
        catch (Exception ex)
        {
            loggerComponentProvider.Log(LogLevel.Error, $"Configuration file: {file.FullName}, Error: {ex.Message}");
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
        }
    }

    private bool CanDownloadInstallationFilesFromAzureStorageAccountCommandHandler()
        => AzureOptions is not null &&
           !string.IsNullOrEmpty(AzureOptions.StorageConnectionString) &&
           !string.IsNullOrEmpty(AzureOptions.BlobContainerName) &&
           ComponentProviders.Count != 0 &&
           NetworkInformationHelper.HasConnection();

    private async Task DownloadInstallationFilesFromAzureStorageAccountCommandHandler()
    {
        if (!CanDownloadInstallationFilesFromAzureStorageAccountCommandHandler())
        {
            return;
        }

        if (!ComponentProviders.Any())
        {
            return;
        }

        IsBusy = true;

        loggerComponentProvider.Log(LogLevel.Trace, "Downloading installation files from Azure StorageAccount");

        var files = await azureStorageAccountInstallerService
            .DownloadLatestFilesByNames(
                AzureOptions!.StorageConnectionString,
                AzureOptions!.BlobContainerName,
                installationDirectory!.FullName,
                GetComponentsWithInstallationFileContentHash())
            .ConfigureAwait(true);

        foreach (var vm in ComponentProviders)
        {
            var fileInfo = files.FirstOrDefault(x => x.Name.StartsWith(vm.Name, StringComparison.OrdinalIgnoreCase));
            if (fileInfo is null)
            {
                continue;
            }

            vm.PrepareInstallationFiles(unpackIfExist: true);
            vm.AnalyzeAndUpdateStatesInBackgroundThread();

            loggerComponentProvider.Log(LogLevel.Information, $"Downloaded installation file: {fileInfo.Name}");
        }

        loggerComponentProvider.Log(LogLevel.Trace, "Downloaded installation files from Azure StorageAccount");

        IsBusy = false;
    }

    private static bool CanOpenApplicationCheckForUpdatesCommandHandler()
        => NetworkInformationHelper.HasConnection();

    private void OpenApplicationCheckForUpdatesCommandHandler()
        => new CheckForUpdatesBoxDialog(checkForUpdatesBoxDialogViewModel).ShowDialog();

    private static void OpenApplicationAboutCommandHandler()
        => new AboutBoxDialog().ShowDialog();

    private bool CanServiceStopAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceStopCommandHandler());

    private Task ServiceStopAllCommandHandler()
    {
        var tasks = new List<Task>();
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceStopCommandHandler())
            {
                tasks.Add(vm.ServiceStopCommand.ExecuteAsync(this));
            }
        }

        return TaskHelper.WhenAll(tasks);
    }

    private bool CanServiceDeployAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceDeployCommandHandler());

    private Task ServiceDeployAllCommandHandler()
    {
        var tasks = new List<Task>();
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceDeployCommandHandler())
            {
                tasks.Add(vm.ServiceDeployCommand.ExecuteAsync(this));
            }
        }

        return TaskHelper.WhenAll(tasks);
    }

    private bool CanServiceStartAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceStartCommandHandler());

    private Task ServiceStartAllCommandHandler()
    {
        var tasks = new List<Task>();
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceStartCommandHandler())
            {
                tasks.Add(vm.ServiceStartCommand.ExecuteAsync(this));
            }
        }

        return TaskHelper.WhenAll(tasks);
    }
}