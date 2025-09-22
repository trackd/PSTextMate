using System.Text;
using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Enhanced code block renderer with span-based optimizations for better performance.
/// Reduces string allocations during code line extraction and processing.
/// </summary>
internal static class EnhancedCodeBlockRenderer
{
    /// <summary>
    /// Renders a fenced code block with span-optimized line extraction.
    /// </summary>
    /// <param name="fencedCode">The fenced code block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rendered code block in a panel</returns>
    public static IRenderable RenderFencedCodeBlock(FencedCodeBlock fencedCode, Theme theme, ThemeName themeName)
    {
        string[]? codeLines = ExtractCodeLinesOptimized(fencedCode.Lines);
        ReadOnlySpan<char> language = ExtractLanguageOptimized(fencedCode.Info);

        if (!language.IsEmpty)
        {
            try
            {
                var rows = TextMateProcessor.ProcessLinesCodeBlock(codeLines, themeName, language.ToString(), false);
                if (rows is not null)
                {
                    return new Panel(rows)
                        .Border(BoxBorder.Rounded)
                        .Header(language.ToString(), Justify.Left);
                }
            }
            catch
            {
                // Fallback to plain rendering below
            }
        }

        // Fallback: plain code panel with optimized text building
        return CreateFallbackCodePanelOptimized(codeLines, language, theme);
    }

    /// <summary>
    /// Extracts code lines using span operations to avoid unnecessary string allocations.
    /// This provides significant performance improvements for large code blocks.
    /// </summary>
    private static string[] ExtractCodeLinesOptimized(Markdig.Helpers.StringLineGroup lines)
    {
        // Pre-allocate array with known size
        string[]? codeLines = new string[lines.Count];
        int index = 0;

        foreach (var line in lines.Lines)
        {
            Markdig.Helpers.StringSlice slice = line.Slice;
            // Use span to avoid intermediate string creation
            ReadOnlySpan<char> lineSpan = slice.Text.AsSpan(slice.Start, slice.Length);
            codeLines[index++] = lineSpan.ToString();
        }

        return codeLines;
    }

    /// <summary>
    /// Extracts language identifier using span operations for better performance.
    /// </summary>
    private static ReadOnlySpan<char> ExtractLanguageOptimized(string? info)
    {
        if (string.IsNullOrEmpty(info))
            return ReadOnlySpan<char>.Empty;

        ReadOnlySpan<char> infoSpan = info.AsSpan().Trim();

        // Find first whitespace to extract just the language part
        int spaceIndex = infoSpan.IndexOfAny([' ', '\t', '\n', '\r']);
        if (spaceIndex >= 0)
            return infoSpan[..spaceIndex];

        return infoSpan;
    }

    /// <summary>
    /// Creates a fallback code panel with span-optimized text concatenation.
    /// </summary>
    private static Panel CreateFallbackCodePanelOptimized(string[] codeLines, ReadOnlySpan<char> language, Theme theme)
    {
        // Calculate total capacity to avoid StringBuilder reallocations
        int totalCapacity = 0;
        foreach (var line in codeLines)
            totalCapacity += line.Length + 1; // +1 for newline

        var textBuilder = new StringBuilder(totalCapacity);

        // Use span-aware joining for better performance
        for (int i = 0; i < codeLines.Length; i++)
        {
            if (i > 0) textBuilder.Append('\n');
            textBuilder.Append(codeLines[i].AsSpan());
        }

        string? fallbackText = Markup.Escape(textBuilder.ToString());
        Style? fallbackStyle = theme.ToSpectreStyle();
        string? headerText = !language.IsEmpty ? language.ToString() : "code";

        return new Panel(new Markup(fallbackText, fallbackStyle))
            .Border(BoxBorder.Rounded)
            .Header(headerText, Justify.Left);
    }
}
