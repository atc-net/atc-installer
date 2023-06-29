namespace Atc.Installer.Wpf.App.Dialogs;

public class CheckForUpdatesBoxDialogViewModel : ViewModelBase, ICheckForUpdatesBoxDialogViewModel
{
    private readonly IGitHubReleaseService gitHubReleaseService;
    private string latestVersion = string.Empty;
    private string latestLink = string.Empty;
    private bool hasNewVersion;

    public CheckForUpdatesBoxDialogViewModel(
        IGitHubReleaseService gitHubReleaseService)
    {
        this.gitHubReleaseService = gitHubReleaseService;

        CurrentVersion = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version!
            .ToString();

        LatestVersion = CurrentVersion;

        TaskHelper.RunSync(RetrieveLatestFromGitHubHandler);
    }

    public IRelayCommandAsync DownloadLatestCommand
        => new RelayCommandAsync(
            DownloadLatestCommandHandler,
            CanDownloadLatestCommandHandler);

    public string CurrentVersion { get; set; }

    public string LatestVersion
    {
        get => latestVersion;
        set
        {
            latestVersion = value;
            RaisePropertyChanged();
        }
    }

    public string LatestLink
    {
        get => latestLink;
        set
        {
            latestLink = value;
            RaisePropertyChanged();

            HasNewVersion = false;
            if (Version.TryParse(CurrentVersion, out var cv) &&
                Version.TryParse(LatestVersion, out var lv))
            {
                HasNewVersion = lv.GreaterThan(cv);
            }
        }
    }

    public bool HasNewVersion
    {
        get => hasNewVersion;
        set
        {
            hasNewVersion = value;
            RaisePropertyChanged();
        }
    }

    private async Task RetrieveLatestFromGitHubHandler()
    {
        var version = await gitHubReleaseService
            .GetLatestVersion()
            .ConfigureAwait(true);

        if (version is not null)
        {
            LatestVersion = version.ToString();
            var link = await gitHubReleaseService
                .GetLatestMsiLink()
                .ConfigureAwait(true);

            if (link is not null)
            {
                LatestLink = link.AbsoluteUri;
            }
        }
    }

    private bool CanDownloadLatestCommandHandler()
        => HasNewVersion;

    private async Task DownloadLatestCommandHandler()
    {
        var downloadBytes = await gitHubReleaseService
            .DownloadFileByLink(new Uri(LatestLink))
            .ConfigureAwait(true);

        if (downloadBytes.Length > 0)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = "Atc.Installer.msi",
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await File
                    .WriteAllBytesAsync(saveFileDialog.FileName, downloadBytes)
                    .ConfigureAwait(true);
            }
        }
    }
}