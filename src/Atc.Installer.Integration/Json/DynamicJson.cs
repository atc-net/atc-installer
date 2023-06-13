// ReSharper disable TailRecursiveCall
namespace Atc.Installer.Integration.Json;

public class DynamicJson
{
    public IDictionary<string, object?> JsonDictionary { get; }

    public DynamicJson(
        string jsonString)
    {
        ArgumentException.ThrowIfNullOrEmpty(jsonString);

        var jsonSerializerOptions = Serialization.JsonSerializerOptionsFactory.Create();
        jsonSerializerOptions.Converters.Add(new JsonElementObjectConverter());

        JsonDictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString, jsonSerializerOptions)!;
    }

    public (bool IsSucceeded, string? ErrorMessage) SetValue(
        string path,
        object? value,
        bool createKeyIfNotExist = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var segments = path.Split('.');
        return SetValueRecursive(
            JsonDictionary,
            segments,
            0,
            value,
            createKeyIfNotExist);
    }

    public string ToJson(
        bool orderByKey = false)
        => orderByKey
            ? JsonSerializer.Serialize(
                new SortedDictionary<string, object?>(JsonDictionary, StringComparer.Ordinal),
                Serialization.JsonSerializerOptionsFactory.Create())
            : JsonSerializer.Serialize(
                JsonDictionary,
                Serialization.JsonSerializerOptionsFactory.Create());

    public override string ToString()
        => ToJson();

    private static (bool IsSucceeded, string? ErrorMessage) SetValueRecursive(
        IDictionary<string, object?> currentDict,
        string[] segments,
        int index,
        object? value,
        bool createKeyIfNotExist)
    {
        var key = segments[index];

        if (index == segments.Length - 1)
        {
            currentDict[key] = value;
            return (
                IsSucceeded: true,
                ErrorMessage: null);
        }

        if (createKeyIfNotExist &&
            !currentDict.ContainsKey(key))
        {
            currentDict.Add(key, new Dictionary<string, object?>(StringComparer.Ordinal));
        }

        if (currentDict[key] is Dictionary<string, object?> nestedDict)
        {
            return SetValueRecursive(
                nestedDict,
                segments,
                index + 1,
                value,
                createKeyIfNotExist);
        }

        var errorInPath = string.Join('.', segments, 0, index + 1);
        return (
            IsSucceeded: false,
            ErrorMessage: $"The path does not exist: {errorInPath}");
    }
}