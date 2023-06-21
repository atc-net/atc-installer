// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum ComponentEndpointType
{
    [Description("Unknown")]
    Unknown,

    [Description("BrowserLink")]
    BrowserLink,

    [Description("ReportingAssemblyInfo")]
    ReportingAssemblyInfo,

    [Description("ReportingHealthCheck")]
    ReportingHealthCheck,
}