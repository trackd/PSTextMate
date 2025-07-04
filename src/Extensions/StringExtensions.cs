namespace PwshSpectreConsole.TextMate.Extensions;

/// <summary>
/// Provides optimized string manipulation methods using modern .NET performance patterns.
/// Uses Span<T> and ReadOnlySpan<T> to minimize memory allocations during text processing.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Efficiently extracts substring using Span to avoid string allocations.
    /// This is significantly faster than traditional substring operations for large text processing.
    /// </summary>
    /// <param name="source">Source string to extract from</param>
    /// <param name="startIndex">Starting index for substring</param>
    /// <param name="endIndex">Ending index for substring</param>
    /// <returns>ReadOnlySpan representing the substring</returns>
    public static ReadOnlySpan<char> SubstringAsSpan(this string source, int startIndex, int endIndex)
    {
        if (startIndex < 0 || endIndex > source.Length || startIndex > endIndex)
        {
            return ReadOnlySpan<char>.Empty;
        }

        return source.AsSpan(startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// Optimized substring method that works with spans internally but returns a string.
    /// Provides better performance than traditional substring while maintaining string return type.
    /// </summary>
    /// <param name="source">Source string to extract from</param>
    /// <param name="startIndex">Starting index for substring</param>
    /// <param name="endIndex">Ending index for substring</param>
    /// <returns>Substring as string, or empty string if invalid indexes</returns>
    public static string SubstringAtIndexes(this string source, int startIndex, int endIndex)
    {
        var span = source.SubstringAsSpan(startIndex, endIndex);
        return span.IsEmpty ? string.Empty : span.ToString();
    }

    /// <summary>
    /// Checks if all strings in the array are null or empty.
    /// Uses modern pattern matching for cleaner, more efficient code.
    /// </summary>
    /// <param name="strings">Array of strings to check</param>
    /// <returns>True if all strings are null or empty, false otherwise</returns>
    public static bool AllIsNullOrEmpty(this string[] strings)
    {
        return strings.All(string.IsNullOrEmpty);
    }
}
