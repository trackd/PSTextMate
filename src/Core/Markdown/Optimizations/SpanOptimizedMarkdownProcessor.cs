using System.Text;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Optimizations;

/// <summary>
/// Provides span-optimized operations for markdown validation and input processing.
/// Reduces allocations during text analysis and validation operations.
/// </summary>
internal static class SpanOptimizedMarkdownProcessor
{
    private static readonly char[] LineBreakChars = ['\r', '\n'];
    private static readonly char[] WhitespaceChars = [' ', '\t', '\r', '\n'];

    /// <summary>
    /// Counts lines in markdown text using span operations for better performance.
    /// </summary>
    /// <param name="markdown">Markdown text to analyze</param>
    /// <returns>Number of lines</returns>
    public static int CountLinesOptimized(ReadOnlySpan<char> markdown)
    {
        if (markdown.IsEmpty) return 0;

        int lineCount = 1; // Start with 1 for the first line
        int index = 0;

        while ((index = markdown[index..].IndexOfAny(LineBreakChars)) >= 0)
        {
            // Handle CRLF as single line break
            if (index < markdown.Length - 1 &&
                markdown[index] == '\r' &&
                markdown[index + 1] == '\n')
            {
                index += 2;
            }
            else
            {
                index++;
            }

            lineCount++;

            if (index >= markdown.Length) break;
        }

        return lineCount;
    }

    /// <summary>
    /// Splits markdown into lines using span operations and returns string array.
    /// Optimized to minimize allocations during the splitting process.
    /// </summary>
    /// <param name="markdown">Markdown text to split</param>
    /// <returns>Array of line strings</returns>
    public static string[] SplitIntoLinesOptimized(ReadOnlySpan<char> markdown)
    {
        if (markdown.IsEmpty) return [];

        int lineCount = CountLinesOptimized(markdown);
        string[]? lines = new string[lineCount];
        int lineIndex = 0;
        int start = 0;

        for (int i = 0; i < markdown.Length; i++)
        {
            bool isLineBreak = markdown[i] is '\r' or '\n';

            if (isLineBreak)
            {
                lines[lineIndex++] = markdown[start..i].ToString();

                // Handle CRLF
                if (i < markdown.Length - 1 && markdown[i] == '\r' && markdown[i + 1] == '\n')
                    i++; // Skip the \n in \r\n

                start = i + 1;
            }
        }

        // Add the last line if it doesn't end with a line break
        if (start < markdown.Length && lineIndex < lines.Length)
            lines[lineIndex] = markdown[start..].ToString();

        return lines;
    }

    /// <summary>
    /// Finds the maximum line length using span operations.
    /// </summary>
    /// <param name="markdown">Markdown text to analyze</param>
    /// <returns>Maximum line length</returns>
    public static int FindMaxLineLengthOptimized(ReadOnlySpan<char> markdown)
    {
        if (markdown.IsEmpty) return 0;

        int maxLength = 0;
        int currentLength = 0;

        foreach (char c in markdown)
        {
            if (c is '\r' or '\n')
            {
                maxLength = Math.Max(maxLength, currentLength);
                currentLength = 0;
            }
            else
            {
                currentLength++;
            }
        }

        // Check the last line
        return Math.Max(maxLength, currentLength);
    }

    /// <summary>
    /// Efficiently trims whitespace from multiple lines using spans.
    /// </summary>
    /// <param name="lines">Array of line strings</param>
    /// <returns>Array of trimmed lines</returns>
    public static string[] TrimLinesOptimized(string[] lines)
    {
        string[]? trimmedLines = new string[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
            {
                trimmedLines[i] = string.Empty;
                continue;
            }

            ReadOnlySpan<char> trimmed = lines[i].AsSpan().Trim();
            trimmedLines[i] = trimmed.Length == lines[i].Length ? lines[i] : trimmed.ToString();
        }

        return trimmedLines;
    }

    /// <summary>
    /// Joins lines back into markdown using span-optimized operations.
    /// </summary>
    /// <param name="lines">Lines to join</param>
    /// <param name="lineEnding">Line ending to use (default: \n)</param>
    /// <returns>Joined markdown text</returns>
    public static string JoinLinesOptimized(ReadOnlySpan<string> lines, ReadOnlySpan<char> lineEnding = default)
    {
        if (lines.IsEmpty) return string.Empty;
        if (lines.Length == 1) return lines[0] ?? string.Empty;

        ReadOnlySpan<char> ending = lineEnding.IsEmpty ? "\n".AsSpan() : lineEnding;

        // Calculate total capacity
        int totalLength = (lines.Length - 1) * ending.Length;
        foreach (var line in lines)
            totalLength += line?.Length ?? 0;

        var builder = new StringBuilder(totalLength);

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) builder.Append(ending);
            if (lines[i] is not null)
                builder.Append(lines[i].AsSpan());
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes empty lines efficiently using span operations.
    /// </summary>
    /// <param name="lines">Lines to filter</param>
    /// <returns>Array with empty lines removed</returns>
    public static string[] RemoveEmptyLinesOptimized(string[] lines)
    {
        // First pass: count non-empty lines
        int nonEmptyCount = 0;
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line) && !line.AsSpan().Trim().IsEmpty)
                nonEmptyCount++;
        }

        if (nonEmptyCount == lines.Length) return lines; // No empty lines
        if (nonEmptyCount == 0) return []; // All empty

        // Second pass: copy non-empty lines
        string[]? result = new string[nonEmptyCount];
        int index = 0;

        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line) && !line.AsSpan().Trim().IsEmpty)
                result[index++] = line;
        }

        return result;
    }

    /// <summary>
    /// Counts specific characters in markdown using span operations.
    /// </summary>
    /// <param name="markdown">Markdown text to analyze</param>
    /// <param name="targetChar">Character to count</param>
    /// <returns>Number of occurrences</returns>
    public static int CountCharacterOptimized(ReadOnlySpan<char> markdown, char targetChar)
    {
        if (markdown.IsEmpty) return 0;

        int count = 0;
        int index = 0;

        while ((index = markdown[index..].IndexOf(targetChar)) >= 0)
        {
            count++;
            index++;
            if (index >= markdown.Length) break;
        }

        return count;
    }
}
