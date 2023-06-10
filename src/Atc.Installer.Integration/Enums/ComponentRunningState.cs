// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum ComponentRunningState
{
    [Description("Unknown")]
    Unknown,

    [Description("Checking")]
    Checking,

    [Description("Not available")]
    NotAvailable,

    [Description("Stopped")]
    Stopped,

    [Description("Partial running")]
    PartialRunning,

    [Description("Running")]
    Running,
}