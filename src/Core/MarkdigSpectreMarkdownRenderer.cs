using Spectre.Console;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Legacy wrapper for the refactored markdown renderer.
/// Now uses the optimized renderer that builds Spectre.Console objects directly.
/// This eliminates VT escaping issues and improves performance.
/// </summary>
internal static class MarkdigSpectreMarkdownRenderer
{
    /// <summary>
    /// Renders markdown content using the optimized Spectre.Console object building approach.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rows object for Spectre.Console rendering</returns>
    public static Rows Render(string markdown, Theme theme, ThemeName themeName)
    {
        return Markdown.OptimizedMarkdownRenderer.Render(markdown, theme, themeName);
    }
}
