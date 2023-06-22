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

    public static IRelayCommand OpenApplicationAboutCommand
        => new RelayCommand(
            OpenApplicationAboutCommandHandler);

    private async Task OpenConfigurationFileCommandHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = false,
            Filter = "Configuration Files(.json)|*.json",
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
            new ApplicationSettingsDialogViewModel(
                ApplicationOptions,
                installerTempDirectory)).ShowDialog();
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

        var componentNames = ComponentProviders
            .Select(x => x.Name)
            .ToArray();

        if (!componentNames.Any())
        {
            return;
        }

        IsBusy = true;

        var files = await azureStorageAccountInstallerService
            .DownloadLatestFilesByNames(
                AzureOptions!.StorageConnectionString,
                AzureOptions!.BlobContainerName,
                installationDirectory!.FullName,
                componentNames)
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

    private static void OpenApplicationAboutCommandHandler()
        => new AboutBoxDialog().ShowDialog();
}