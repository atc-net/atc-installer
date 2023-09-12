// ReSharper disable CheckNamespace
namespace System;

public static class StringExtensions
{
    public static bool ContainsTemplateKeyBrackets(
        this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Contains("[[", StringComparison.Ordinal) &&
               value.Contains("]]", StringComparison.Ordinal);
    }

    [SuppressMessage("Performance", "MA0110:Use the Regex source generator", Justification = "OK.")]
    public static IList<string> GetTemplateKeys(
        this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var doubleBracketContentPattern = new Regex(@"\[\[(.*?)\]\]", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
        var matches = doubleBracketContentPattern.Matches(value);
        return matches
            .Select(match => match
                .Groups[0]
                .Value
                .Replace("[[", string.Empty, StringComparison.Ordinal)
                .Replace("]]", string.Empty, StringComparison.Ordinal))
            .ToList();
    }

    public static IList<string> SplitTemplate(
        this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var list = new List<string>();
        var sa1 = value.Split("[[", StringSplitOptions.RemoveEmptyEntries);
        foreach (var sx1 in sa1)
        {
            var sa2 = sx1.Split("]]", StringSplitOptions.RemoveEmptyEntries);
            list.AddRange(sa2);
        }

        return list;
    }

    public static string ReplaceTemplateWithKey(
        this string value,
        string templateKey,
        string newValue)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Replace($"[[{templateKey}]]", newValue, StringComparison.OrdinalIgnoreCase);
    }

    public static string IndentEachLineWith(
        this string value,
        string prefixPadding)
    {
        var lines = value.ToLines();
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append(prefixPadding);
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}