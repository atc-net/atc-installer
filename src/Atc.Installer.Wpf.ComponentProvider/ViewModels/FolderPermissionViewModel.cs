namespace Atc.Installer.Wpf.ComponentProvider.ViewModels;

public class FolderPermissionViewModel : ViewModelBase
{
    private string user = string.Empty;
    private FileSystemRights fileSystemRights = FileSystemRights.ReadData;
    private string folder = string.Empty;
    private DirectoryInfo? directory;

    public FolderPermissionViewModel()
    {
    }

    public FolderPermissionViewModel(
        FolderPermissionOption folderPermission)
    {
        ArgumentNullException.ThrowIfNull(folderPermission);

        User = folderPermission.User;
        FileSystemRights = Enum<FileSystemRights>.Parse(folderPermission.FileSystemRights);

        Folder = folderPermission.Folder;
        Directory = new DirectoryInfo(folderPermission.Folder);
    }

    public string User
    {
        get => user;
        set
        {
            user = value;
            RaisePropertyChanged();
        }
    }

    public FileSystemRights FileSystemRights
    {
        get => fileSystemRights;
        set
        {
            fileSystemRights = value;
            RaisePropertyChanged();
        }
    }

    public string Folder
    {
        get => folder;
        set
        {
            folder = value;
            RaisePropertyChanged();
        }
    }

    public DirectoryInfo? Directory
    {
        get => directory;
        set
        {
            directory = value;
            RaisePropertyChanged();
        }
    }

    public override string ToString()
        => $"{nameof(User)}: {User}, {nameof(FileSystemRights)}: {FileSystemRights}, {nameof(Folder)}: {Folder}, {nameof(Directory)}: {Directory}";
}