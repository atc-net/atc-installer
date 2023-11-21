// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum HostingFrameworkType
{
    None,

    [Description("Native")]
    Native,

    [Description("Native")]
    NativeNoSettings,

    [Description(".NET Framework 4.8")]
    DonNetFramework48,

    [Description(".NET 7")]
    DotNet7,

    [Description(".NET 8")]
    DotNet8,

    [Description("NodeJS")]
    NodeJs,
}