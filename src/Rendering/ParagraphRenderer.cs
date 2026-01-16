using System.Text;
using System.Text.RegularExpressions;
using Markdig.Extensions;
using Markdig.Extensions.AutoLinks;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;
using PSTextMate.Utilities;
using PSTextMate.Core;

namespace PSTextMate.Rendering;

/// <summary>
/// Paragraph renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static partial class ParagraphRenderer {
    // reuse static arrays for common scope queries to avoid allocating new arrays per call
    private static readonly string[] LinkScope = ["markup.underline.link"];

    /// <summary>
    /// Renders a paragraph block by building Spectre.Console Paragraph objects directly.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="paragraph">The paragraph block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered paragraph as a Paragraph object with proper inline styling</returns>
    public static IRenderable Render(ParagraphBlock paragraph, Theme theme) {
        var spectreParagraph = new Paragraph();

        if (paragraph.Inline is not null) {
            ProcessInlineElements(spectreParagraph, paragraph.Inline, theme);
        }

        return spectreParagraph;
    }

    /// <summary>
    /// Processes inline elements and adds them directly to the Paragraph with appropriate styling.
    /// </summary>
    /// <param name="paragraph">Target Spectre paragraph to append to</param>
    /// <param name="inlines">Markdig inline container</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="skipLineBreaks">If true, skips LineBreakInline (used for list items where Rows handles spacing)</param>
    internal static void ProcessInlineElements(Paragraph paragraph, ContainerInline inlines, Theme theme, bool skipLineBreaks = false) {
        // Convert to list to allow index-based access for checking trailing line breaks
        List<Inline> inlineList = [.. inlines];

        for (int i = 0; i < inlineList.Count; i++) {
            Inline inline = inlineList[i];

            // Check if this is a trailing line break (last element or followed only by other line breaks)
            bool isTrailingLineBreak = false;
            if (inline is LineBreakInline && i < inlineList.Count) {
                isTrailingLineBreak = true;
                // Check if there are any non-LineBreakInline elements after this
                for (int j = i + 1; j < inlineList.Count; j++) {
                    if (inlineList[j] is not LineBreakInline) {
                        isTrailingLineBreak = false;
                        break;
                    }
                }
            }

            switch (inline) {
                case LiteralInline literal:
                    string literalText = literal.Content.ToString();
                    // Skip empty literals to avoid extra blank lines
                    if (string.IsNullOrEmpty(literalText)) {
                        break;
                    }

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out TextSegment[]? segments)) {
                        foreach (TextSegment segment in segments) {
                            if (segment.IsUsername) {
                                // Create clickable username link (you could customize the URL pattern)
                                var usernameStyle = new Style(
                                    foreground: Color.Blue,
                                    decoration: Decoration.Underline,
                                    link: $"https://github.com/{segment.Text.TrimStart('@')}"
                                );
                                paragraph.Append(segment.Text, usernameStyle);
                            }
                            else {
                                paragraph.Append(segment.Text, Style.Plain);
                            }
                        }
                    }
                    else {
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

                case AutolinkInline autoLink:
                    ProcessAutoLinkInline(paragraph, autoLink, theme);
                    break;

                case TaskList taskList:
                    // TaskList items are handled at the list level, skip here
                    break;

                case LineBreakInline:
                    // Skip trailing line breaks to avoid double-spacing with Rows container
                    // Also skip line breaks in lists (Rows handles spacing)
                    if (!skipLineBreaks && !isTrailingLineBreak) {
                        paragraph.Append("\n", Style.Plain);
                    }
                    break;

                case HtmlInline html:
                    // For HTML inlines, just extract the text content
                    string htmlText = html.Tag ?? "";
                    paragraph.Append(htmlText, Style.Plain);
                    break;

                default:
                    // Fallback for unknown inline types - just write text as-is
                    string defaultText = ExtractInlineText(inline);
                    paragraph.Append(defaultText, Style.Plain);
                    break;
            }
        }
    }

    /// <summary>
    /// Processes emphasis (bold/italic) inline elements while preserving nested links.
    /// </summary>
    private static void ProcessEmphasisInline(Paragraph paragraph, EmphasisInline emphasis, Theme theme) {
        // Determine emphasis style based on delimiter count
        Decoration decoration = emphasis.DelimiterCount switch {
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
    private static void ProcessInlineElementsWithDecoration(Paragraph paragraph, ContainerInline container, Decoration decoration, Theme theme) {
        foreach (Inline inline in container) {
            switch (inline) {
                case LiteralInline literal:
                    string literalText = literal.Content.ToString();
                    var emphasisStyle = new Style(decoration: decoration);

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out TextSegment[]? segments)) {
                        foreach (TextSegment segment in segments) {
                            if (segment.IsUsername) {
                                // Create clickable username link with emphasis
                                var usernameStyle = new Style(
                                    foreground: Color.Blue,
                                    decoration: Decoration.Underline | decoration, // Combine with emphasis
                                    link: $"https://github.com/{segment.Text.TrimStart('@')}"
                                );
                                paragraph.Append(segment.Text, usernameStyle);
                            }
                            else {
                                paragraph.Append(segment.Text, emphasisStyle);
                            }
                        }
                    }
                    else {
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
                    Decoration nestedDecoration = nestedEmphasis.DelimiterCount switch {
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
                    string defaultText = ExtractInlineText(inline);
                    paragraph.Append(defaultText, new Style(decoration: decoration));
                    break;
            }
        }
    }

    /// <summary>
    /// Processes a link inline while applying emphasis decoration.
    /// </summary>
    private static void ProcessLinkInlineWithDecoration(Paragraph paragraph, LinkInline link, Decoration emphasisDecoration, Theme theme) {
        // Use link text if available, otherwise use URL
        string linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText)) {
            linkText = link.Url ?? "";
        }

        // Use cached style if available
        Style? linkStyleBase = TokenProcessor.GetStyleForScopes(LinkScope, theme);
        Color? foregroundColor = linkStyleBase?.Foreground ?? Color.Blue;
        Color? backgroundColor = linkStyleBase?.Background ?? null;
        Decoration baseDecoration = linkStyleBase is not null ? linkStyleBase.Decoration : Decoration.None;
        Decoration linkDecoration = baseDecoration | Decoration.Underline | emphasisDecoration;

        var linkStyle = new Style(foregroundColor, backgroundColor, linkDecoration, link.Url);
        paragraph.Append(linkText, linkStyle);
    }

    /// <summary>
    /// Processes inline code elements with syntax highlighting.
    /// </summary>
    private static void ProcessCodeInline(Paragraph paragraph, CodeInline code, Theme theme) {
        // Get theme colors for inline code
        string[] codeScopes = ["markup.inline.raw"];
        (int codeFg, int codeBg, FontStyle codeFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(codeScopes), theme);

        // Create code styling
        Color? foregroundColor = codeFg != -1 ? StyleHelper.GetColor(codeFg, theme) : Color.Yellow;
        Color? backgroundColor = codeBg != -1 ? StyleHelper.GetColor(codeBg, theme) : Color.Grey11;
        Decoration decoration = StyleHelper.GetDecoration(codeFs);

        var codeStyle = new Style(foregroundColor, backgroundColor, decoration);
        paragraph.Append(code.Content, codeStyle);
    }

    /// <summary>
    /// Processes link inline elements with clickable links using Spectre.Console Style with link parameter.
    /// Also handles images (when IsImage is true) by delegating to ImageRenderer.
    /// </summary>
    private static void ProcessLinkInline(Paragraph paragraph, LinkInline link, Theme theme) {
        // Check if this is an image (![alt](url) syntax)
        if (link.IsImage) {
            // Extract alt text from the link
            string altText = ExtractInlineText(link);
            if (string.IsNullOrEmpty(altText)) {
                altText = "Image";
            }

            // Render the image using ImageRenderer (Sixel support)
            IRenderable imageRenderable = ImageRenderer.RenderImageInline(altText, link.Url ?? "", maxWidth: null, maxHeight: null);

            // Note: Can't directly append IRenderable to Paragraph, so we need to handle this differently
            // For now, images inside paragraphs will use fallback link representation
            // TODO: Consider restructuring to support embedded IRenderable in Paragraph
            if (imageRenderable is Markup imageMarkup) {
                // If it's a fallback Markup, we can append it
                string markupText = imageMarkup.ToString() ?? "";
                paragraph.Append(markupText, Style.Plain);
            }
            else {
                // It's a SixelImage - can't embed in Paragraph inline
                // Fall back to link representation
                string imageLinkText = $"🖼️ {altText}";
                var imageLinkStyle = new Style(
                    foreground: Color.Blue,
                    decoration: Decoration.Underline,
                    link: link.Url
                );
                paragraph.Append(imageLinkText, imageLinkStyle);
            }
            return;
        }

        // Regular link handling
        // Use link text if available, otherwise use URL
        string linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText)) {
            linkText = link.Url ?? "";
        }

        // Get theme colors for links
        string[] linkScopes = ["markup.underline.link"];
        (int linkFg, int linkBg, FontStyle linkFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(linkScopes), theme);

        // Create link styling with clickable URL
        Color? foregroundColor = linkFg != -1 ? StyleHelper.GetColor(linkFg, theme) : Color.Blue;
        Color? backgroundColor = linkBg != -1 ? StyleHelper.GetColor(linkBg, theme) : null;
        Decoration decoration = StyleHelper.GetDecoration(linkFs) | Decoration.Underline;

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
    /// Processes Markdig AutolinkInline (URLs/emails detected by UseAutoLinks).
    /// </summary>
    private static void ProcessAutoLinkInline(Paragraph paragraph, AutolinkInline autoLink, Theme theme) {
        string url = autoLink.Url ?? string.Empty;
        if (string.IsNullOrEmpty(url)) {
            // Nothing to render
            return;
        }

        // Get theme colors for links
        string[] linkScopes = ["markup.underline.link"];
        (int linkFg, int linkBg, FontStyle linkFs) = TokenProcessor.ExtractThemeProperties(
            new MarkdownToken(linkScopes), theme);

        Color? foregroundColor = linkFg != -1 ? StyleHelper.GetColor(linkFg, theme) : Color.Blue;
        Color? backgroundColor = linkBg != -1 ? StyleHelper.GetColor(linkBg, theme) : null;
        Decoration decoration = StyleHelper.GetDecoration(linkFs) | Decoration.Underline;

        var linkStyle = new Style(
            foreground: foregroundColor,
            background: backgroundColor,
            decoration: decoration,
            link: url
        );

        // For autolinks, the visible text is the URL itself
        paragraph.Append(url, linkStyle);
    }

    /// <summary>
    /// Extracts plain text from inline elements without markup.
    /// </summary>
    private static string ExtractInlineText(Inline inline) {
        StringBuilder builder = StringBuilderPool.Rent();
        try {
            InlineTextExtractor.ExtractText(inline, builder);
            return builder.ToString();
        }
        finally {
            StringBuilderPool.Return(builder);
        }
    }

    /// <summary>
    /// Represents a text segment that may or may not be a username link.
    /// </summary>
    private sealed record TextSegment(string Text, bool IsUsername);

    /// <summary>
    /// Tries to parse username links (@username) from literal text.
    /// </summary>
    private static bool TryParseUsernameLinks(string text, out TextSegment[] segments) {
        var segmentList = new List<TextSegment>();

        // Simple regex to find @username patterns
        Regex usernamePattern = RegNumLet();
        MatchCollection matches = usernamePattern.Matches(text);

        if (matches.Count == 0) {
            segments = [];
            return false;
        }

        int lastIndex = 0;
        foreach (Match match in matches) {
            // Add text before the username
            if (match.Index > lastIndex) {
                segmentList.Add(new TextSegment(text[lastIndex..match.Index], false));
            }

            // Add the username
            segmentList.Add(new TextSegment(match.Value, true));
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length) {
            segmentList.Add(new TextSegment(text[lastIndex..], false));
        }

        segments = [.. segmentList];
        return true;
    }



    [GeneratedRegex(@"@[a-zA-Z0-9_-]+")]
    private static partial Regex RegNumLet();
}
