// ReSharper disable CheckNamespace
namespace System;

public static class StringExtensions
{
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