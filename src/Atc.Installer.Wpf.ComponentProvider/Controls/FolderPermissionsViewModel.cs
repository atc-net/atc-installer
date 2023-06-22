namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class FolderPermissionsViewModel : ViewModelBase
{
    public FolderPermissionsViewModel()
    {
    }

    public FolderPermissionsViewModel(
        IList<FolderPermissionOption> folderPermissions)
    {
        Populate(folderPermissions);
    }

    public ObservableCollectionEx<FolderPermissionViewModel> Items { get; init; } = new();

    public void Populate(
        IList<FolderPermissionOption> folderPermissions)
    {
        ArgumentNullException.ThrowIfNull(folderPermissions);

        Items.Clear();

        Items.SuppressOnChangedNotification = true;

        foreach (var folderPermission in folderPermissions)
        {
            Items.Add(new FolderPermissionViewModel(folderPermission));
        }

        Items.SuppressOnChangedNotification = false;
    }
}