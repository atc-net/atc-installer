// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum ComponentInstallationState
{
    [Description("Unknown")]
    Unknown,

    [Description("Checking")]
    Checking,

    [Description("No installations files")]
    NoInstallationsFiles,

    [Description("Not installed")]
    NotInstalled,

    [Description("Installing")]
    Installing,

    [Description("Installed with old version")]
    InstalledWithOldVersion,

    [Description("Installed with newest version")]
    InstalledWithNewestVersion,
}