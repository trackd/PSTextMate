using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Block renderer that uses Spectre.Console object building instead of markup strings.
/// This eliminates VT escaping issues and improves performance by avoiding double-parsing.
/// </summary>
internal static class BlockRenderer {
    /// <summary>
    /// Routes block elements to their appropriate renderers.
    /// All renderers build Spectre.Console objects directly instead of markup strings.
    /// </summary>
    /// <param name="block">The block element to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rendered block as a Spectre.Console object, or null if unsupported</returns>
    public static IRenderable? RenderBlock(Block block, Theme theme, ThemeName themeName) {
        return block switch {
            // Special handling for paragraphs that contain only an image
            ParagraphBlock paragraph when IsStandaloneImage(paragraph) => RenderStandaloneImage(paragraph, theme),

            // Use renderers that build Spectre.Console objects directly
            HeadingBlock heading => HeadingRenderer.Render(heading, theme),
            ParagraphBlock paragraph => ParagraphRenderer.Render(paragraph, theme),
            ListBlock list => ListRenderer.Render(list, theme),
            Table table => TableRenderer.Render(table, theme),
            FencedCodeBlock fencedCode => CodeBlockRenderer.RenderFencedCodeBlock(fencedCode, theme, themeName),
            CodeBlock indentedCode => CodeBlockRenderer.RenderCodeBlock(indentedCode, theme),

            // Keep existing renderers for remaining complex blocks
            QuoteBlock quote => QuoteRenderer.Render(quote, theme),
            HtmlBlock html => HtmlBlockRenderer.Render(html, theme, themeName),
            ThematicBreakBlock => HorizontalRuleRenderer.Render(),

            // Unsupported block types
            _ => null
        };
    }

    /// <summary>
    /// Checks if a paragraph block contains only a single image (no other text).
    /// </summary>
    private static bool IsStandaloneImage(ParagraphBlock paragraph) {
        if (paragraph.Inline is null) {
            return false;
        }

        // Check if the paragraph contains only one LinkInline with IsImage = true
        var inlines = paragraph.Inline.ToList();

        // Single image case
        if (inlines.Count == 1 && inlines[0] is LinkInline link && link.IsImage) {
            return true;
        }

        // Sometimes there might be whitespace inlines around the image
        // Filter out empty/whitespace literals
        var nonWhitespace = inlines
            .Where(i => i is not LineBreakInline && !(i is LiteralInline lit && string.IsNullOrWhiteSpace(lit.Content.ToString())))
            .ToList();

    bool result = nonWhitespace.Count == 1
            && nonWhitespace[0] is LinkInline imageLink
            && imageLink.IsImage;
    return result;
    }

    /// <summary>
    /// Renders a standalone image (paragraph containing only an image).
    /// Demonstrates how SixelImage can be directly rendered or wrapped in containers.
    /// </summary>
    private static IRenderable? RenderStandaloneImage(ParagraphBlock paragraph, Theme theme) {
        if (paragraph.Inline is null) {
            return null;
        }

        // Find the image link
        LinkInline? imageLink = paragraph.Inline
            .OfType<LinkInline>()
            .FirstOrDefault(link => link.IsImage);

        if (imageLink is null) {
            return null;
        }

        // Extract alt text
        string altText = ExtractImageAltText(imageLink);

        // Render using ImageBlockRenderer which handles various layouts
        // Can render as: Direct (most common), PanelWithCaption, WithPadding, etc.
        // This demonstrates how SixelImage (an IRenderable) can be embedded in different containers:
        // - Panel: Wrap with border and title
        // - Columns: Side-by-side layout
        // - Rows: Vertical stacking
        // - Grid: Flexible grid layout
        // - Table: Inside table cells
        // - Or rendered directly without wrapper

        return ImageBlockRenderer.RenderImageBlock(
            altText,
            imageLink.Url ?? "",
            renderMode: ImageRenderMode.Direct);  // Direct rendering is most efficient
    }

    /// <summary>
    /// Extracts alt text from an image link inline.
    /// </summary>
    private static string ExtractImageAltText(LinkInline imageLink) {
        var textBuilder = new System.Text.StringBuilder();

        foreach (Inline inline in imageLink) {
            if (inline is LiteralInline literal) {
                textBuilder.Append(literal.Content.ToString());
            }
            else if (inline is CodeInline code) {
                textBuilder.Append(code.Content);
            }
        }

        string result = textBuilder.ToString();
        return string.IsNullOrEmpty(result) ? "Image" : result;
    }
}
