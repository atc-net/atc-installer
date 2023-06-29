// ReSharper disable InvertIf
namespace Atc.Installer.Wpf.App.Dialogs;

/// <summary>
/// Interaction logic for CheckForUpdatesBoxDialog.
/// </summary>
public partial class CheckForUpdatesBoxDialog
{
    public CheckForUpdatesBoxDialog(
        ICheckForUpdatesBoxDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOk(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}