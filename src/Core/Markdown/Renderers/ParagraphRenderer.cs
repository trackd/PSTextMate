using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Paragraph renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static class ParagraphRenderer
{
    /// <summary>
    /// Renders a paragraph block by building Spectre.Console Paragraph objects directly.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="paragraph">The paragraph block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered paragraph as a Paragraph object with proper inline styling</returns>
    public static IRenderable Render(ParagraphBlock paragraph, Theme theme)
    {
        var spectreConsole = new Paragraph();

        if (paragraph.Inline is not null)
        {
            ProcessInlineElements(spectreConsole, paragraph.Inline, theme);
        }

        return spectreConsole;
    }

    /// <summary>
    /// Processes inline elements and adds them directly to the Paragraph with appropriate styling.
    /// </summary>
    private static void ProcessInlineElements(Paragraph paragraph, ContainerInline inlines, Theme theme)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    var literalText = literal.Content.ToString();
                    paragraph.Append(literalText, Style.Plain);
                    break;

                case EmphasisInline emphasis:
                    ProcessEmphasisInline(paragraph, emphasis, theme);
                    break;

                case CodeInline code:
                    ProcessCodeInline(paragraph, code, theme);
                    break;

                case LinkInline link:
                    ProcessLinkInline(paragraph, link, theme);
                    break;

                case Markdig.Extensions.TaskLists.TaskList taskList:
                    // TaskList items are handled at the list level, skip here
                    break;

                case LineBreakInline:
                    paragraph.Append("\n", Style.Plain);
                    break;

                case HtmlInline html:
                    // For HTML inlines, just extract the text content
                    var htmlText = html.Tag ?? "";
                    paragraph.Append(htmlText, Style.Plain);
                    break;

                default:
                    // Fallback for unknown inline types - extract text
                    var defaultText = ExtractInlineText(inline);
                    paragraph.Append(defaultText, Style.Plain);
                    break;
            }
        }
    }

    /// <summary>
    /// Processes emphasis (bold/italic) inline elements.
    /// </summary>
    private static void ProcessEmphasisInline(Paragraph paragraph, EmphasisInline emphasis, Theme theme)
    {
        // Determine emphasis style based on delimiter count
        var decoration = emphasis.DelimiterCount switch
        {
            1 => Decoration.Italic,      // Single * or _
            2 => Decoration.Bold,        // Double ** or __
            3 => Decoration.Bold | Decoration.Italic, // Triple *** or ___
            _ => Decoration.None
        };

        var emphasisStyle = new Style(decoration: decoration);
        var emphasisText = ExtractInlineText(emphasis);

        paragraph.Append(emphasisText, emphasisStyle);
    }

    /// <summary>
    /// Processes inline code elements with syntax highlighting.
    /// </summary>
    private static void ProcessCodeInline(Paragraph paragraph, CodeInline code, Theme theme)
    {
        // Get theme colors for inline code
        var codeScopes = new[] { "markup.inline.raw" };
        var (codeFg, codeBg, codeFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(codeScopes), theme);

        // Create code styling
        Color? foregroundColor = codeFg != -1 ? StyleHelper.GetColor(codeFg, theme) : Color.Yellow;
        Color? backgroundColor = codeBg != -1 ? StyleHelper.GetColor(codeBg, theme) : Color.Grey11;
        var decoration = StyleHelper.GetDecoration(codeFs);

        var codeStyle = new Style(foregroundColor, backgroundColor, decoration);
        paragraph.Append(code.Content, codeStyle);
    }

    /// <summary>
    /// Processes link inline elements.
    /// </summary>
    private static void ProcessLinkInline(Paragraph paragraph, LinkInline link, Theme theme)
    {
        // Get theme colors for links
        var linkScopes = new[] { "markup.underline.link" };
        var (linkFg, linkBg, linkFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(linkScopes), theme);

        // Create link styling
        Color? foregroundColor = linkFg != -1 ? StyleHelper.GetColor(linkFg, theme) : Color.Blue;
        Color? backgroundColor = linkBg != -1 ? StyleHelper.GetColor(linkBg, theme) : null;
        var decoration = StyleHelper.GetDecoration(linkFs) | Decoration.Underline;

        var linkStyle = new Style(foregroundColor, backgroundColor, decoration);

        // Use link text if available, otherwise use URL
        var linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText))
        {
            linkText = link.Url ?? "";
        }

        paragraph.Append(linkText, linkStyle);
    }

    /// <summary>
    /// Extracts plain text from inline elements without markup.
    /// </summary>
    private static string ExtractInlineText(Inline inline)
    {
        var builder = new System.Text.StringBuilder();
        ExtractInlineTextRecursive(inline, builder);
        return builder.ToString();
    }

    /// <summary>
    /// Recursively extracts text from inline elements.
    /// </summary>
    private static void ExtractInlineTextRecursive(Inline inline, System.Text.StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case ContainerInline container:
                foreach (var child in container)
                {
                    ExtractInlineTextRecursive(child, builder);
                }
                break;

            case LeafInline leaf:
                if (leaf is CodeInline code)
                {
                    builder.Append(code.Content);
                }
                else if (leaf is LineBreakInline)
                {
                    builder.Append('\n');
                }
                break;
        }
    }
}
