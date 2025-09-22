using System.Text;
using Markdig.Syntax.Inlines;
using PwshSpectreConsole.TextMate.Core.Helpers;
using PwshSpectreConsole.TextMate.Extensions;
using Spectre.Console;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown;

/// <summary>
/// Handles extraction and styling of inline markdown elements.
/// </summary>
internal static class InlineProcessor
{
    /// <summary>
    /// Extracts and styles inline text from Markdig inline elements.
    /// </summary>
    /// <param name="container">Container holding inline elements</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="builder">StringBuilder to append results to</param>
    public static void ExtractInlineText(ContainerInline? container, Theme theme, StringBuilder builder)
    {
        if (container is null) return;

        foreach (Inline inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    ProcessLiteralInline(literal, builder);
                    break;

                case LinkInline link:
                    ProcessLinkInline(link, theme, builder);
                    break;

                case EmphasisInline emph:
                    ProcessEmphasisInline(emph, theme, builder);
                    break;

                case CodeInline code:
                    ProcessCodeInline(code, theme, builder);
                    break;

                case LineBreakInline:
                    builder.Append('\n');
                    break;

                default:
                    if (inline is ContainerInline childContainer)
                        ExtractInlineText(childContainer, theme, builder);
                    break;
            }
        }
    }

    /// <summary>
    /// Processes literal text inline elements.
    /// </summary>
    private static void ProcessLiteralInline(LiteralInline literal, StringBuilder builder)
    {
        ReadOnlySpan<char> span = literal.Content.Text.AsSpan(literal.Content.Start, literal.Content.Length);
        builder.Append(span);
    }

    /// <summary>
    /// Processes link and image inline elements.
    /// </summary>
    private static void ProcessLinkInline(LinkInline link, Theme theme, StringBuilder builder)
    {
        if (!string.IsNullOrEmpty(link.Url))
        {
            var linkBuilder = new StringBuilder();
            ExtractInlineText(link, theme, linkBuilder);

            if (link.IsImage)
            {
                ProcessImageLink(linkBuilder.ToString(), link.Url, theme, builder);
            }
            else
            {
                builder.AppendLink(link.Url, linkBuilder.ToString());
            }
        }
        else
        {
            ExtractInlineText(link, theme, builder);
        }
    }

    /// <summary>
    /// Processes image links with special styling.
    /// </summary>
    private static void ProcessImageLink(string altText, string url, Theme theme, StringBuilder builder)
    {
        // For now, render images as enhanced fallback since we can't easily make this async
        // In the future, this could be enhanced to support actual Sixel rendering

        // Check if the image format is likely supported
        bool isSupported = ImageFile.IsLikelySupportedImageFormat(url);

        if (isSupported)
        {
            // Enhanced image representation for supported formats
            builder.Append("🖼️ ");
            builder.AppendLink(url, $"Image: {altText} (Sixel-ready)");
        }
        else
        {
            // Basic image representation for unsupported formats
            builder.Append("🖼️ ");
            builder.AppendLink(url, $"Image: {altText}");
        }
    }

    /// <summary>
    /// Processes emphasis inline elements (bold, italic).
    /// </summary>
    private static void ProcessEmphasisInline(EmphasisInline emph, Theme theme, StringBuilder builder)
    {
        string[]? emphScopes = MarkdigTextMateScopeMapper.GetInlineScopes("Emphasis", emph.DelimiterCount);
        (int efg, int ebg, FontStyle efStyle) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(emphScopes), theme);

        var emphBuilder = new StringBuilder();
        ExtractInlineText(emph, theme, emphBuilder);

        // Apply the theme colors/style to the emphasis text
        if (efg != -1 || ebg != -1 || efStyle != TextMateSharp.Themes.FontStyle.NotSet)
        {
            Color emphColor = efg != -1 ? StyleHelper.GetColor(efg, theme) : Color.Default;
            Color emphBgColor = ebg != -1 ? StyleHelper.GetColor(ebg, theme) : Color.Default;
            Decoration emphDecoration = StyleHelper.GetDecoration(efStyle);

            Style? emphStyle = new Style(emphColor, emphBgColor, emphDecoration);
            builder.AppendWithStyle(emphStyle, emphBuilder.ToString());
        }
        else
        {
            builder.Append(emphBuilder);
        }
    }

    /// <summary>
    /// Processes inline code elements.
    /// </summary>
    private static void ProcessCodeInline(CodeInline code, Theme theme, StringBuilder builder)
    {
        string[]? codeScopes = MarkdigTextMateScopeMapper.GetInlineScopes("CodeInline");
        (int cfg, int cbg, FontStyle cfStyle) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(codeScopes), theme);

        // Apply the theme colors/style to the inline code
        if (cfg != -1 || cbg != -1 || cfStyle != TextMateSharp.Themes.FontStyle.NotSet)
        {
            Color codeColor = cfg != -1 ? StyleHelper.GetColor(cfg, theme) : Color.Default;
            Color codeBgColor = cbg != -1 ? StyleHelper.GetColor(cbg, theme) : Color.Default;
            Decoration codeDecoration = StyleHelper.GetDecoration(cfStyle);

            var codeStyle = new Style(codeColor, codeBgColor, codeDecoration);
            builder.AppendWithStyle(codeStyle, code.Content);
        }
        else
        {
            builder.Append(code.Content.EscapeMarkup());
        }
    }
}
