// ReSharper disable CheckNamespace
namespace Atc.Installer.Integration;

public enum ComponentType
{
    None,

    [Description("Application")]
    Application,

    [Description("ElasticSearch")]
    ElasticSearchServer,

    [Description("IIS")]
    InternetInformationService,

    [Description("PostgreSql")]
    PostgreSqlServer,

    [Description("NT-Service")]
    WindowsService,
}