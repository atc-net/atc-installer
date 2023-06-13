// ReSharper disable CheckNamespace
namespace System.Xml;

public static class XmlDocumentExtensions
{
    public static string ToIndentedXml(
        this XmlDocument xmlDocument)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);

        var xDoc = XDocument.Parse(xmlDocument.OuterXml);
        return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{Environment.NewLine}{xDoc}";
    }
}