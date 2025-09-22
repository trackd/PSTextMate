using Markdig.Extensions.AutoLinks;
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
    internal static void ProcessInlineElements(Paragraph paragraph, ContainerInline inlines, Theme theme)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    var literalText = literal.Content.ToString();

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out var segments))
                    {
                        foreach (var segment in segments)
                        {
                            if (segment.IsUsername)
                            {
                                // Create clickable username link (you could customize the URL pattern)
                                var usernameStyle = new Style(
                                    foreground: Color.Blue,
                                    decoration: Decoration.Underline,
                                    link: $"https://github.com/{segment.Text.TrimStart('@')}"
                                );
                                paragraph.Append(segment.Text, usernameStyle);
                            }
                            else
                            {
                                paragraph.Append(segment.Text, Style.Plain);
                            }
                        }
                    }
                    else
                    {
                        paragraph.Append(literalText, Style.Plain);
                    }
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

                // Note: AutoLinkInline handling can be added when we identify the correct class name

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

                    // Check if this might be an AutoLink by looking at the type name
                    var typeName = inline.GetType().Name;

                    // Debug: Check for specific AutoLink types
                    if (typeName.Contains("Auto") || typeName.Contains("Link") ||
                        (!string.IsNullOrEmpty(defaultText) &&
                         (defaultText.StartsWith("http://", StringComparison.Ordinal) ||
                          defaultText.StartsWith("https://", StringComparison.Ordinal) ||
                          defaultText.Contains('@'))))
                    {
                        // Try to extract URL from AutoLink or detect URL patterns
                        string? autoUrl = TryExtractAutoLinkUrl(inline);
                        if (string.IsNullOrEmpty(autoUrl) &&
                            (defaultText.StartsWith("http://", StringComparison.Ordinal) ||
                             defaultText.StartsWith("https://", StringComparison.Ordinal)))
                        {
                            autoUrl = defaultText; // Use the text itself as URL
                        }

                        if (!string.IsNullOrEmpty(autoUrl))
                        {
                            // Create clickable auto-link
                            var autoLinkStyle = new Style(
                                foreground: Color.Blue,
                                decoration: Decoration.Underline,
                                link: autoUrl
                            );
                            paragraph.Append(defaultText, autoLinkStyle);
                        }
                        else
                        {
                            paragraph.Append(defaultText, Style.Plain);
                        }
                    }
                    else
                    {
                        paragraph.Append(defaultText, Style.Plain);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Processes emphasis (bold/italic) inline elements while preserving nested links.
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

        // Process children while applying emphasis decoration
        ProcessInlineElementsWithDecoration(paragraph, emphasis, decoration, theme);
    }

    /// <summary>
    /// Processes inline elements while applying a decoration (like bold/italic) to text elements,
    /// but preserving special handling for links and other complex inlines.
    /// </summary>
    private static void ProcessInlineElementsWithDecoration(Paragraph paragraph, ContainerInline container, Decoration decoration, Theme theme)
    {
        foreach (Inline inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    var literalText = literal.Content.ToString();
                    var emphasisStyle = new Style(decoration: decoration);

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out var segments))
                    {
                        foreach (var segment in segments)
                        {
                            if (segment.IsUsername)
                            {
                                // Create clickable username link with emphasis
                                var usernameStyle = new Style(
                                    foreground: Color.Blue,
                                    decoration: Decoration.Underline | decoration, // Combine with emphasis
                                    link: $"https://github.com/{segment.Text.TrimStart('@')}"
                                );
                                paragraph.Append(segment.Text, usernameStyle);
                            }
                            else
                            {
                                paragraph.Append(segment.Text, emphasisStyle);
                            }
                        }
                    }
                    else
                    {
                        paragraph.Append(literalText, emphasisStyle);
                    }
                    break;

                case LinkInline link:
                    // Process link but apply emphasis decoration to the link text
                    ProcessLinkInlineWithDecoration(paragraph, link, decoration, theme);
                    break;

                case CodeInline code:
                    // Code should not inherit emphasis decoration
                    ProcessCodeInline(paragraph, code, theme);
                    break;

                case EmphasisInline nestedEmphasis:
                    // Handle nested emphasis by combining decorations
                    var nestedDecoration = nestedEmphasis.DelimiterCount switch
                    {
                        1 => Decoration.Italic,
                        2 => Decoration.Bold,
                        3 => Decoration.Bold | Decoration.Italic,
                        _ => Decoration.None
                    };
                    ProcessInlineElementsWithDecoration(paragraph, nestedEmphasis, decoration | nestedDecoration, theme);
                    break;

                case LineBreakInline:
                    paragraph.Append("\n", Style.Plain);
                    break;

                default:
                    // Fallback - apply emphasis to extracted text
                    var defaultText = ExtractInlineText(inline);
                    paragraph.Append(defaultText, new Style(decoration: decoration));
                    break;
            }
        }
    }

    /// <summary>
    /// Processes a link inline while applying emphasis decoration.
    /// </summary>
    private static void ProcessLinkInlineWithDecoration(Paragraph paragraph, LinkInline link, Decoration emphasisDecoration, Theme theme)
    {
        // Use link text if available, otherwise use URL
        var linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText))
        {
            linkText = link.Url ?? "";
        }

        // Get theme colors for links
        var linkScopes = new[] { "markup.underline.link" };
        var (linkFg, linkBg, linkFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(linkScopes), theme);

        // Create link styling with emphasis decoration combined
        Color? foregroundColor = linkFg != -1 ? StyleHelper.GetColor(linkFg, theme) : Color.Blue;
        Color? backgroundColor = linkBg != -1 ? StyleHelper.GetColor(linkBg, theme) : null;
        var linkDecoration = StyleHelper.GetDecoration(linkFs) | Decoration.Underline | emphasisDecoration;

        // Create style with link parameter for clickable links
        var linkStyle = new Style(
            foreground: foregroundColor,
            background: backgroundColor,
            decoration: linkDecoration,
            link: link.Url // This makes it clickable!
        );

        paragraph.Append(linkText, linkStyle);
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
    /// Processes link inline elements with clickable links using Spectre.Console Style with link parameter.
    /// </summary>
    private static void ProcessLinkInline(Paragraph paragraph, LinkInline link, Theme theme)
    {
        // Use link text if available, otherwise use URL
        var linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText))
        {
            linkText = link.Url ?? "";
        }

        // Get theme colors for links
        var linkScopes = new[] { "markup.underline.link" };
        var (linkFg, linkBg, linkFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(linkScopes), theme);

        // Create link styling with clickable URL
        Color? foregroundColor = linkFg != -1 ? StyleHelper.GetColor(linkFg, theme) : Color.Blue;
        Color? backgroundColor = linkBg != -1 ? StyleHelper.GetColor(linkBg, theme) : null;
        var decoration = StyleHelper.GetDecoration(linkFs) | Decoration.Underline;

        // Create style with link parameter for clickable links
        var linkStyle = new Style(
            foreground: foregroundColor,
            background: backgroundColor,
            decoration: decoration,
            link: link.Url // This makes it clickable!
        );

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
    /// Represents a text segment that may or may not be a username link.
    /// </summary>
    private sealed record TextSegment(string Text, bool IsUsername);

    /// <summary>
    /// Tries to parse username links (@username) from literal text.
    /// </summary>
    private static bool TryParseUsernameLinks(string text, out TextSegment[] segments)
    {
        var segmentList = new List<TextSegment>();

        // Simple regex to find @username patterns
        var usernamePattern = new System.Text.RegularExpressions.Regex(@"@[a-zA-Z0-9_-]+");
        var matches = usernamePattern.Matches(text);

        if (matches.Count == 0)
        {
            segments = [];
            return false;
        }

        int lastIndex = 0;
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            // Add text before the username
            if (match.Index > lastIndex)
            {
                segmentList.Add(new TextSegment(text[lastIndex..match.Index], false));
            }

            // Add the username
            segmentList.Add(new TextSegment(match.Value, true));
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            segmentList.Add(new TextSegment(text[lastIndex..], false));
        }

        segments = segmentList.ToArray();
        return true;
    }

    /// <summary>
    /// Tries to extract URL from potential AutoLink inlines using reflection.
    /// </summary>
    private static string? TryExtractAutoLinkUrl(Inline inline)
    {
        try
        {
            // Try common AutoLink property names
            var urlProperty = inline.GetType().GetProperty("Url");
            if (urlProperty is not null)
            {
                return urlProperty.GetValue(inline) as string;
            }

            var linkProperty = inline.GetType().GetProperty("Link");
            if (linkProperty is not null)
            {
                return linkProperty.GetValue(inline) as string;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
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
