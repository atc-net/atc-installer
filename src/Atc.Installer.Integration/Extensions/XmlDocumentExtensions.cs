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

    public static void SetValue(
        this XmlDocument xmlDocument,
        string path,
        string elementName,
        string? attributeName,
        string? attributeValue)
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(elementName);

        var nodes = path
            .Split(':')
            .ToList();
        nodes.Add(elementName);

        var currentElement = xmlDocument.DocumentElement!;

        foreach (var node in nodes.Where(x => x != "configuration"))
        {
            if (currentElement.SelectSingleNode(node) is not XmlElement nextElement)
            {
                nextElement = xmlDocument.CreateElement(node);
                currentElement.AppendChild(nextElement);
            }

            currentElement = nextElement;
        }

        if (attributeName is null ||
            attributeValue is null)
        {
            return;
        }

        var attribute = currentElement.GetAttributeNode(attributeName);
        if (attribute is null)
        {
            attribute = xmlDocument.CreateAttribute(attributeName);
            currentElement.Attributes.Append(attribute);
        }

        attribute.Value = attributeValue;
    }

    public static void SetAppSettings(
        this XmlDocument xmlDocument,
        string key,
        string value,
        string elementName = "add")
    {
        ArgumentNullException.ThrowIfNull(xmlDocument);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);

        if (xmlDocument.DocumentElement!.SelectSingleNode("appSettings") is not XmlElement appSettingsElement)
        {
            appSettingsElement = xmlDocument.CreateElement("appSettings");
            xmlDocument.DocumentElement.AppendChild(appSettingsElement);
        }

        if (appSettingsElement.SelectSingleNode($"{elementName}[@key='{key}']") is not XmlElement addElement)
        {
            addElement = xmlDocument.CreateElement(elementName);
            addElement.SetAttribute("key", key);
            addElement.SetAttribute("value", value);
            appSettingsElement.AppendChild(addElement);
        }
        else
        {
            addElement.SetAttribute("value", value);
        }
    }
}