namespace Atc.Installer.Integration.InstallationConfigurations;

public class FolderPermissionOption
{
    public string User { get; set; } = string.Empty;

    public string FileSystemRights { get; set; } = string.Empty;

    public string Folder { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(User)}: {User}, {nameof(FileSystemRights)}: {FileSystemRights}, {nameof(Folder)}: {Folder}";
}