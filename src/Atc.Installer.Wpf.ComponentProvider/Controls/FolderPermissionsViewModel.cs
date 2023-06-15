namespace Atc.Installer.Wpf.ComponentProvider.Controls;

public class FolderPermissionsViewModel : ViewModelBase
{
    public ObservableCollectionEx<FolderPermissionViewModel> Items { get; set; } = new();

    public void Populate(
        IList<FolderPermissionOption> folderPermissions)
    {
        ArgumentNullException.ThrowIfNull(folderPermissions);

        Items.SuppressOnChangedNotification = true;

        Items.Clear();

        foreach (var folderPermission in folderPermissions)
        {
            Items.Add(new FolderPermissionViewModel(folderPermission));
        }

        Items.SuppressOnChangedNotification = false;
    }
}