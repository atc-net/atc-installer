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
        new ApplicationSettingsDialog(
            new ApplicationSettingsDialogViewModel(ApplicationOptions)).ShowDialog();
    }

    private Task OpenRecentConfigurationFileCommandHandler(
        string filePath)
        => LoadConfigurationFile(
            new FileInfo(filePath),
            CancellationToken.None);

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
        }

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

    private async Task ServiceStopAllCommandHandler()
    {
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceStopCommandHandler())
            {
                await vm.ServiceStopCommand
                    .ExecuteAsync(this)
                    .ConfigureAwait(false);
            }
        }
    }

    private bool CanServiceDeployAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceDeployCommandHandler());

    private async Task ServiceDeployAllCommandHandler()
    {
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceDeployCommandHandler())
            {
                await vm.ServiceDeployCommand
                    .ExecuteAsync(this)
                    .ConfigureAwait(false);
            }
        }
    }

    private bool CanServiceStartAllCommandHandler()
        => ComponentProviders.Any(x => x.CanServiceStartCommandHandler());

    private async Task ServiceStartAllCommandHandler()
    {
        foreach (var vm in ComponentProviders)
        {
            if (vm.CanServiceStartCommandHandler())
            {
                await vm.ServiceStartCommand
                    .ExecuteAsync(this)
                    .ConfigureAwait(false);
            }
        }
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

    public static string CalculateMd5(
        FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(file.FullName);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}