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

    public IRelayCommandAsync ReportingToExcelCommand => new RelayCommandAsync(
        ReportingToExcelCommandHandler,
        CanReportingToExcelCommandHandler);

    public IRelayCommand OpenApplicationCheckForUpdatesCommand
        => new RelayCommand(
            OpenApplicationCheckForUpdatesCommandHandler,
            CanOpenApplicationCheckForUpdatesCommandHandler);

    public IRelayCommand OpenApplicationAboutCommand
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

    public IRelayCommandAsync ServiceRemoveAllCommand
        => new RelayCommandAsync(
            ServiceRemoveAllCommandHandler,
            CanServiceRemoveAllCommandHandler);

    public IRelayCommandAsync ServiceStartAllCommand
        => new RelayCommandAsync(
            ServiceStartAllCommandHandler,
            CanServiceStartAllCommandHandler);

    public new ICommand ApplicationExitCommand
        => new RelayCommand(ApplicationExitCommandHandler);

    private async Task OpenConfigurationFileCommandHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            InitialDirectory = App.InstallerProgramDataProjectsDirectory.FullName,
            Multiselect = false,
            Title = "Select a Installation-Settings file",
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
        => ApplicationOptions.EnableEditingMode &&
           InstallationFile is not null &&
           (IsDirty ||
            ComponentProviders.Any(x => x.IsDirty ||
                                        x.DefaultApplicationSettings.IsDirty ||
                                        x.ApplicationSettings.IsDirty ||
                                        x.FolderPermissions.IsDirty ||
                                        x.FirewallRules.IsDirty ||
                                        x.ConfigurationSettingsFiles.IsDirty));

    private Task SaveConfigurationFileCommandHandler()
        => SaveConfigurationFile();

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

        var reloadProjectInstallationFile = false;
        var templateSettingsFileContentHash = GetTemplateSettingsWithInstallationFileContentHash(installationDirectory!);
        if (templateSettingsFileContentHash is not null)
        {
            var projectNameTerms = ProjectName!.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var list = new List<(string ComponentName, string? ContentHash)>
            {
                ("Settings", templateSettingsFileContentHash),
                ($"{projectNameTerms[0]}.Settings", templateSettingsFileContentHash),
            };

            if (projectNameTerms.Length > 1)
            {
                list.Add(($"{projectNameTerms[0]}.{projectNameTerms[1]}.Settings", templateSettingsFileContentHash));
            }

            var files = await azureStorageAccountInstallerService
                .DownloadLatestFilesByNames(
                    AzureOptions!.StorageConnectionString,
                    AzureOptions!.BlobContainerName,
                    installationDirectory!.FullName,
                    list)
                .ConfigureAwait(true);

            if (files.Any())
            {
                reloadProjectInstallationFile = await ConfigurationFileHelper
                    .UpdateInstallationSettingsFromCustomAndTemplateSettingsIfNeeded(installationDirectory)
                    .ConfigureAwait(true);
            }
        }

        var componentFiles = await azureStorageAccountInstallerService
            .DownloadLatestFilesByNames(
                AzureOptions!.StorageConnectionString,
                AzureOptions!.BlobContainerName,
                installationDirectory!.FullName,
                GetComponentsWithInstallationFileContentHash())
            .ConfigureAwait(true);

        foreach (var vm in ComponentProviders)
        {
            var fileInfo = componentFiles.FirstOrDefault(x => x.Name.StartsWith(vm.Name, StringComparison.OrdinalIgnoreCase));
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

        if (reloadProjectInstallationFile)
        {
            await LoadConfigurationFile(InstallationFile!, CancellationToken.None)
                .ConfigureAwait(true);
        }
    }

    private bool CanReportingToExcelCommandHandler()
        => InstallationFile is not null &&
           (!IsDirty ||
            !ComponentProviders.Any(x => x.IsDirty ||
                                         x.DefaultApplicationSettings.IsDirty ||
                                         x.ApplicationSettings.IsDirty ||
                                         x.FolderPermissions.IsDirty ||
                                         x.FirewallRules.IsDirty ||
                                         x.ConfigurationSettingsFiles.IsDirty));

    private async Task ReportingToExcelCommandHandler()
    {
        if (!CanReportingToExcelCommandHandler())
        {
            return;
        }

        if (!ComponentProviders.Any())
        {
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            Title = "Select a file",
            Filter = "Excel Files|*.xlsx",
            FileName = ProjectName,
        };

        if (saveFileDialog.ShowDialog() != true ||
            saveFileDialog.FileName is null)
        {
            return;
        }

        var exportToFile = new FileInfo(saveFileDialog.FileName);
        if (exportToFile.Exists)
        {
            try
            {
                File.Delete(exportToFile.FullName);
            }
            catch (Exception)
            {
                var infoDialogBox = new InfoDialogBox(
                    Application.Current.MainWindow!,
                    "Error",
                    $"Can't override file,{Environment.NewLine}because it is being used by{Environment.NewLine}another process.");
                infoDialogBox.ShowDialog();
                return;
            }
        }

        IsBusy = true;

        // Give the UI a moment to refresh (show BusyIndicator).
        await Task
            .Delay(1, CancellationToken.None)
            .ConfigureAwait(false);

        var tasks = ComponentProviders
            .Select(vm => vm.GetReportingData())
            .ToList();

        try
        {
            var reportingDataForComponentProviders = await TaskHelper
                .WhenAll(tasks)
                .ConfigureAwait(true);

            ExcelHelper.CreateAndSave(exportToFile, reportingDataForComponentProviders);
        }
        catch (Exception ex)
        {
            var infoDialogBox = new InfoDialogBox(
                Application.Current.MainWindow!,
                "Error",
                ex.Message);
            infoDialogBox.ShowDialog();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool CanOpenApplicationCheckForUpdatesCommandHandler()
        => NetworkInformationHelper.HasConnection();

    private void OpenApplicationCheckForUpdatesCommandHandler()
        => new CheckForUpdatesBoxDialog(checkForUpdatesBoxDialogViewModel).ShowDialog();

    private void OpenApplicationAboutCommandHandler()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var aboutBoxDialog = new AboutBoxDialog();
        aboutBoxDialog.IconImage.Source = ApplicationOptions.Icon ?? App.DefaultIcon;
        aboutBoxDialog.ShowDialog();
    }

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

    private bool CanServiceRemoveAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceRemoveCommandHandler());

    private Task ServiceRemoveAllCommandHandler()
    {
        var tasks = new List<Task>();
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceRemoveCommandHandler())
            {
                tasks.Add(vm.ServiceRemoveCommand.ExecuteAsync(this));
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

    private void ApplicationExitCommandHandler()
    {
        if (CanSaveConfigurationFileCommandHandler())
        {
            var dialogBox = new QuestionDialogBox(
                Application.Current.MainWindow!,
                "Unsaved data",
                "Are you sure you want to exit without saving changes?")
            {
                Width = 500,
            };

            dialogBox.ShowDialog();

            if (!dialogBox.DialogResult.HasValue ||
                !dialogBox.DialogResult.Value)
            {
                return;
            }
        }

        OnClosing(this, new CancelEventArgs());
    }
}