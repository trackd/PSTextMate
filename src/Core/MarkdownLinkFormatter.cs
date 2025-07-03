using Spectre.Console;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Provides specialized formatting for Markdown elements.
/// Handles conversion of Markdown syntax to Spectre Console markup.
/// </summary>
internal static class MarkdownLinkFormatter
{
    /// <summary>
    /// Creates a markdown link with Spectre Console markup.
    /// </summary>
    /// <param name="url">URL for the link</param>
    /// <param name="linkText">Display text for the link</param>
    /// <returns>Formatted link markup</returns>
    public static string WriteMarkdownLink(string url, string linkText)
    {
        return $"[Blue link={url}]{linkText}[/] ";
    }

    /// <summary>
    /// Creates a markdown link with style information.
    /// </summary>
    /// <param name="url">URL for the link</param>
    /// <param name="linkText">Display text for the link</param>
    /// <returns>Tuple of formatted link and style</returns>
    public static (string textEscaped, Style style) WriteMarkdownLinkWithStyle(string url, string linkText)
    {
        string mdlink = $"[link={url}]{Markup.Escape(linkText)}[/]";
        Style style = new(Color.Blue, Color.Default);
        return (mdlink, style);
    }
}
