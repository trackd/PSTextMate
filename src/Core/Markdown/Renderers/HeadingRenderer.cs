using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Helpers;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Heading renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead.
/// </summary>
internal static class HeadingRenderer {
    /// <summary>
    /// Renders a heading block by building Spectre.Console Text objects directly.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="heading">The heading block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered heading as a Text object with proper styling</returns>
    public static IRenderable Render(HeadingBlock heading, Theme theme) {
        // Extract heading text without building markup strings
        string headingText = ExtractHeadingText(heading);

        // Get theme colors for heading styling
        string[] headingScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Heading", heading.Level);
        (int hfg, int hbg, FontStyle hfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(headingScopes), theme);

        // Build styling directly
        Style headingStyle = CreateHeadingStyle(hfg, hbg, hfs, theme, heading.Level);

        // Return Text object directly - no markup parsing needed
        return new Rows(new Paragraph(headingText, headingStyle));
    }

    /// <summary>
    /// Extracts plain text from heading inline elements without building markup.
    /// </summary>
    private static string ExtractHeadingText(HeadingBlock heading) {
        if (heading.Inline is null)
            return "";

        StringBuilder textBuilder = StringBuilderPool.Rent();
        try {

            foreach (Inline inline in heading.Inline) {
                switch (inline) {
                    case LiteralInline literal:
                        textBuilder.Append(literal.Content.ToString());
                        break;

                    case EmphasisInline emphasis:
                        // For headings, we'll just extract the text without emphasis styling
                        // since the heading style takes precedence
                        ExtractInlineTextRecursive(emphasis, textBuilder);
                        break;

                    case CodeInline code:
                        textBuilder.Append(code.Content);
                        break;

                    case LinkInline link:
                        // Extract link text, not the URL
                        ExtractInlineTextRecursive(link, textBuilder);
                        break;

                    default:
                        ExtractInlineTextRecursive(inline, textBuilder);
                        break;
                }
            }

            return textBuilder.ToString();
        }
        finally {
            StringBuilderPool.Return(textBuilder);
        }
    }

    /// <summary>
    /// Recursively extracts text from inline elements.
    /// </summary>
    private static void ExtractInlineTextRecursive(Inline inline, StringBuilder builder) {
        switch (inline) {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case ContainerInline container:
                foreach (Inline child in container) {
                    ExtractInlineTextRecursive(child, builder);
                }
                break;

            case LeafInline leaf:
                if (leaf is CodeInline code) {
                    builder.Append(code.Content);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Creates appropriate styling for headings based on theme and level.
    /// </summary>
    private static Style CreateHeadingStyle(int foreground, int background, FontStyle fontStyle, Theme theme, int level) {
        Color? foregroundColor = null;
        Color? backgroundColor = null;

        // Apply theme colors if available
        if (foreground != -1) {
            foregroundColor = StyleHelper.GetColor(foreground, theme);
        }

        if (background != -1) {
            backgroundColor = StyleHelper.GetColor(background, theme);
        }

        // Apply font style decorations
        Decoration decoration = StyleHelper.GetDecoration(fontStyle);

        // Apply level-specific styling as fallbacks
        foregroundColor ??= GetDefaultHeadingColor(level);

        // Ensure headings are bold by default
        if (decoration == Decoration.None) {
            decoration = Decoration.Bold;
        }

        return new Style(foregroundColor ?? Color.Default, backgroundColor ?? Color.Default, decoration);
    }

    /// <summary>
    /// Gets default colors for heading levels when theme doesn't provide them.
    /// </summary>
    private static Color GetDefaultHeadingColor(int level) {
        return level switch {
            1 => Color.Red,
            2 => Color.Orange1,
            3 => Color.Yellow,
            4 => Color.Green,
            5 => Color.Blue,
            6 => Color.Purple,
            _ => Color.White
        };
    }
}
