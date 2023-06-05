namespace Atc.Installer.Integration;

public interface IInstallerService
{
    bool IsInstalled { get; }

    bool IsRunning { get; }
}