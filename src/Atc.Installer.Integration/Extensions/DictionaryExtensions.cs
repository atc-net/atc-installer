namespace Atc.Installer.Integration.Extensions;

public static class DictionaryExtensions
{
    public static bool TryGetStringFromDictionary(
        this IDictionary<string, object> dictionary,
        string key,
        out string value)
    {
        if (dictionary is not null &&
            dictionary.TryGetValue(key, out var objValue) &&
            objValue is not null)
        {
            value = objValue.ToString()!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static bool TryGetUshortFromDictionary(
        this IDictionary<string, object> dictionary,
        string key,
        out ushort value)
    {
        if (dictionary is not null &&
            dictionary.TryGetValue(key, out var objValue) &&
            ushort.TryParse(
                objValue.ToString(),
                NumberStyles.Number,
                GlobalizationConstants.EnglishCultureInfo,
                out var retrievedValue))
        {
            value = retrievedValue;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetIntFromDictionary(
        this IDictionary<string, object> dictionary,
        string key,
        out int value)
    {
        if (dictionary is not null &&
            dictionary.TryGetValue(key, out var objValue) &&
            int.TryParse(
                objValue.ToString(),
                NumberStyles.Number,
                GlobalizationConstants.EnglishCultureInfo,
                out var retrievedValue))
        {
            value = retrievedValue;
            return true;
        }

        value = default;
        return false;
    }
}