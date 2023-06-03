// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum ComponentInstallationState
{
    Unknown,
    Checking,
    NoInstallationsFiles,
    NotInstalled,
    InstalledWithOldVersion,
    InstalledWithNewestVersion,
}