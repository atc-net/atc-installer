// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.App.Dialogs;

/// <summary>
/// Interaction logic for CheckForUpdatesBoxDialog.
/// </summary>
public partial class CheckForUpdatesBoxDialog
{
    public CheckForUpdatesBoxDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        VersionTextBlock.Text = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version!
            .ToString();

        NoVersionUpdatesContainer.Visibility = Visibility.Visible;
        LatestVersionContainer.Visibility = Visibility.Collapsed;
        LatestLinkTextBlock.Visibility = Visibility.Collapsed;
        LatestVersionTextBlock.Text = VersionTextBlock.Text;
    }

    private void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        var gitHubReleaseService = new GitHubReleaseService();
        if (NetworkInformationHelper.HasConnection())
        {
            var version = TaskHelper.RunSync(() => gitHubReleaseService.GetLatestVersion());
            if (version is not null)
            {
                LatestVersionTextBlock.Text = version.ToString();
                var link = TaskHelper.RunSync(() => gitHubReleaseService.GetLatestMsiLink());
                LatestVersionHyperlink.NavigateUri = link;

                var currentVersion = new Version(VersionTextBlock.Text);
                if (version.GreaterThan(currentVersion))
                {
                    NoVersionUpdatesContainer.Visibility = Visibility.Collapsed;
                    LatestVersionContainer.Visibility = Visibility.Visible;
                    LatestLinkTextBlock.Visibility = Visibility.Visible;
                }
            }
        }
    }

    private void OnLatestVersionHyperlinkClick(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink latestVersionHyperlink &&
            latestVersionHyperlink.NavigateUri.IsAbsoluteUri)
        {
            InternetBrowserHelper.OpenUrl(latestVersionHyperlink.NavigateUri);
        }
    }

    private void OnOk(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}