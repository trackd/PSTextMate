using PwshSpectreConsole.TextMate.Core.Markdown;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Provides specialized rendering for Markdown content using the modern Markdig-based renderer.
/// This facade delegates to the Core.Markdown.MarkdownRenderer which builds Spectre.Console objects directly.
/// </summary>
/// <remarks>
/// Legacy string-based renderer was removed in favor of the object-based Markdig renderer for better performance
/// and to eliminate VT escape sequence issues.
/// </remarks>
internal static class MarkdownRenderer {
    /// <summary>
    /// Renders Markdown content with special handling for links and enhanced formatting.
    /// </summary>
    /// <param name="lines">Lines to render</param>
    /// <param name="theme">Theme to apply</param>
    /// <param name="grammar">Markdown grammar (used for compatibility, actual rendering uses Markdig)</param>
    /// <param name="themeName">Theme name for passing to Markdig renderer</param>
    /// <param name="debugCallback">Optional debug callback (not used by Markdig renderer)</param>
    /// <returns>Rendered rows with markdown syntax highlighting</returns>
    public static Rows Render(string[] lines, Theme theme, IGrammar grammar, ThemeName themeName, Action<TokenDebugInfo>? debugCallback) {
        string markdown = string.Join("\n", lines);
        return Markdown.MarkdownRenderer.Render(markdown, theme, themeName);
    }
}
