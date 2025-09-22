using System.Text;

namespace PwshSpectreConsole.TextMate.Extensions;

/// <summary>
/// Enhanced string manipulation methods optimized with Span operations.
/// Provides significant performance improvements for text processing scenarios.
/// </summary>
public static class SpanOptimizedStringExtensions
{
    /// <summary>
    /// Joins string arrays using span operations for better performance.
    /// Avoids multiple string allocations during concatenation.
    /// </summary>
    /// <param name="values">Array of strings to join</param>
    /// <param name="separator">Separator character</param>
    /// <returns>Joined string</returns>
    public static string JoinOptimized(this string[] values, char separator)
    {
        if (values.Length == 0) return string.Empty;
        if (values.Length == 1) return values[0] ?? string.Empty;

        // Calculate total capacity to avoid StringBuilder reallocations
        int totalLength = values.Length - 1; // separators
        foreach (string value in values)
            totalLength += value?.Length ?? 0;

        var builder = new StringBuilder(totalLength);

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0) builder.Append(separator);
            if (values[i] is not null)
                builder.Append(values[i].AsSpan());
        }

        return builder.ToString();
    }

    /// <summary>
    /// Joins string arrays with string separator using span operations.
    /// </summary>
    /// <param name="values">Array of strings to join</param>
    /// <param name="separator">Separator string</param>
    /// <returns>Joined string</returns>
    public static string JoinOptimized(this string[] values, string separator)
    {
        if (values.Length == 0) return string.Empty;
        if (values.Length == 1) return values[0] ?? string.Empty;

        // Calculate total capacity
        int separatorLength = separator?.Length ?? 0;
        int totalLength = (values.Length - 1) * separatorLength;
        foreach (string value in values)
            totalLength += value?.Length ?? 0;

        var builder = new StringBuilder(totalLength);

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0 && separator is not null)
                builder.Append(separator.AsSpan());
            if (values[i] is not null)
                builder.Append(values[i].AsSpan());
        }

        return builder.ToString();
    }

    /// <summary>
    /// Splits strings using span operations with pre-allocated results array.
    /// Provides better performance for known maximum split counts.
    /// </summary>
    /// <param name="source">Source string to split</param>
    /// <param name="separators">Array of separator characters</param>
    /// <param name="options">String split options</param>
    /// <param name="maxSplits">Maximum expected number of splits for optimization</param>
    /// <returns>Array of split strings</returns>
    public static string[] SplitOptimized(this string source, char[] separators, StringSplitOptions options = StringSplitOptions.None, int maxSplits = 16)
    {
        if (string.IsNullOrEmpty(source))
            return [];

        // Use span-based operations for better performance
        ReadOnlySpan<char> sourceSpan = source.AsSpan();
        var results = new List<string>(Math.Min(maxSplits, 64)); // Cap initial capacity

        int start = 0;
        for (int i = 0; i <= sourceSpan.Length; i++)
        {
            bool isSeparator = i < sourceSpan.Length && separators.Contains(sourceSpan[i]);
            bool isEnd = i == sourceSpan.Length;

            if (isSeparator || isEnd)
            {
                ReadOnlySpan<char> segment = sourceSpan[start..i];

                if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && segment.IsEmpty)
                {
                    start = i + 1;
                    continue;
                }

                if (options.HasFlag(StringSplitOptions.TrimEntries))
                    segment = segment.Trim();

                results.Add(segment.ToString());
                start = i + 1;
            }
        }

        return results.ToArray();
    }

    /// <summary>
    /// Trims whitespace using span operations and returns the result as a string.
    /// More efficient than traditional Trim() for subsequent string operations.
    /// </summary>
    /// <param name="source">Source string to trim</param>
    /// <returns>Trimmed string</returns>
    public static string TrimOptimized(this string source)
    {
        if (string.IsNullOrEmpty(source))
            return source ?? string.Empty;

        ReadOnlySpan<char> trimmed = source.AsSpan().Trim();
        return trimmed.Length == source.Length ? source : trimmed.ToString();
    }

    /// <summary>
    /// Efficiently checks if a string contains any of the specified characters using spans.
    /// </summary>
    /// <param name="source">Source string to search</param>
    /// <param name="chars">Characters to search for</param>
    /// <returns>True if any character is found</returns>
    public static bool ContainsAnyOptimized(this string source, ReadOnlySpan<char> chars)
    {
        if (string.IsNullOrEmpty(source) || chars.IsEmpty)
            return false;

        return source.AsSpan().IndexOfAny(chars) >= 0;
    }

    /// <summary>
    /// Replaces characters in a string using span operations for better performance.
    /// </summary>
    /// <param name="source">Source string</param>
    /// <param name="oldChar">Character to replace</param>
    /// <param name="newChar">Replacement character</param>
    /// <returns>String with replacements</returns>
    public static string ReplaceOptimized(this string source, char oldChar, char newChar)
    {
        if (string.IsNullOrEmpty(source))
            return source ?? string.Empty;

        ReadOnlySpan<char> sourceSpan = source.AsSpan();
        int firstIndex = sourceSpan.IndexOf(oldChar);

        if (firstIndex < 0)
            return source; // No replacement needed

        // Use span-based building for efficiency
        var result = new StringBuilder(source.Length);
        int lastIndex = 0;

        do
        {
            result.Append(sourceSpan[lastIndex..firstIndex]);
            result.Append(newChar);
            lastIndex = firstIndex + 1;

            if (lastIndex >= sourceSpan.Length)
                break;

            firstIndex = sourceSpan[lastIndex..].IndexOf(oldChar);
            if (firstIndex >= 0)
                firstIndex += lastIndex;

        } while (firstIndex >= 0);

        if (lastIndex < sourceSpan.Length)
            result.Append(sourceSpan[lastIndex..]);

        return result.ToString();
    }
}
