using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Legacy wrapper for the refactored markdown renderer.
/// Maintains backward compatibility while delegating to the new modular implementation.
/// </summary>
internal static class MarkdigSpectreMarkdownRenderer
{
    /// <summary>
    /// Renders markdown content using Markdig and Spectre.Console.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rows object for Spectre.Console rendering</returns>
    public static Rows Render(string markdown, Theme theme, ThemeName themeName)
    {
        return Markdown.MarkdownRenderer.Render(markdown, theme, themeName);
    }
}
