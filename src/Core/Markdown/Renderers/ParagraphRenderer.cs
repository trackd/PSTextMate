﻿using System.Text.RegularExpressions;
using Markdig.Extensions.AutoLinks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions;
using Markdig.Extensions.TaskLists;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;
using System.Text;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Paragraph renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static partial class ParagraphRenderer
{
    // reuse static arrays for common scope queries to avoid allocating new arrays per call
    private static readonly string[] LinkScope = ["markup.underline.link"];

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
                    string literalText = literal.Content.ToString();

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out TextSegment[]? segments))
                    {
                        foreach (TextSegment segment in segments)
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

                case AutolinkInline autoLink:
                    ProcessAutoLinkInline(paragraph, autoLink, theme);
                    break;

                case TaskList taskList:
                    // TaskList items are handled at the list level, skip here
                    break;

                case LineBreakInline:
                    paragraph.Append("\n", Style.Plain);
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
    private static void ProcessEmphasisInline(Paragraph paragraph, EmphasisInline emphasis, Theme theme)
    {
        // Determine emphasis style based on delimiter count
        Decoration decoration = emphasis.DelimiterCount switch
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
                    string literalText = literal.Content.ToString();
                    var emphasisStyle = new Style(decoration: decoration);

                    // Check for username patterns like @username
                    if (TryParseUsernameLinks(literalText, out TextSegment[]? segments))
                    {
                        foreach (TextSegment segment in segments)
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
                    Decoration nestedDecoration = nestedEmphasis.DelimiterCount switch
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
                    string defaultText = ExtractInlineText(inline);
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
        string linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText))
        {
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
    private static void ProcessCodeInline(Paragraph paragraph, CodeInline code, Theme theme)
    {
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
    /// </summary>
    private static void ProcessLinkInline(Paragraph paragraph, LinkInline link, Theme theme)
    {
        // Use link text if available, otherwise use URL
        string linkText = ExtractInlineText(link);
        if (string.IsNullOrEmpty(linkText))
        {
            linkText = link.Url ?? "";
        }

        // Get theme colors for links
        string[] linkScopes = new[] { "markup.underline.link" };
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
    private static void ProcessAutoLinkInline(Paragraph paragraph, AutolinkInline autoLink, Theme theme)
    {
        string url = autoLink.Url ?? string.Empty;
        if (string.IsNullOrEmpty(url))
        {
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
        var usernamePattern = RegNumLet();
        MatchCollection matches = usernamePattern.Matches(text);

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

    private static void ExtractInlineTextRecursive(Inline inline, StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case ContainerInline container:
                foreach (Inline child in container)
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

    [GeneratedRegex(@"@[a-zA-Z0-9_-]+")]
    private static partial Regex RegNumLet();
}
