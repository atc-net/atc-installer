// ReSharper disable CheckNamespace
namespace System;

public static class StringExtensions
{
    public static IList<string> GetTemplateKeys(
        this string value)
    {
        var regex = new Regex(@"\[\[(.*?)\]\]", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
        var matches = regex.Matches(value);
        return matches
            .Select(match => match
                .Groups[0]
                .Value
                .Replace("[[", string.Empty, StringComparison.Ordinal)
                .Replace("]]", string.Empty, StringComparison.Ordinal))
            .ToList();
    }
}