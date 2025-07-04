using System.Linq;
using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Extensions;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders markdown paragraph blocks.
/// </summary>
internal static class ParagraphRenderer
{
    /// <summary>
    /// Renders a paragraph block with theme-aware styling.
    /// </summary>
    /// <param name="paragraph">The paragraph block to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Rendered paragraph markup or empty text if no content</returns>
    public static IRenderable Render(ParagraphBlock paragraph, Theme theme)
    {
        // Check if this paragraph contains any images
        if (ContainsImages(paragraph, out var imageLinks))
        {
            // For paragraphs containing images, use synchronous rendering with Sixel support
            try
            {
                var result = RenderParagraphWithImages(paragraph, theme, imageLinks);
                return result;
            }
            catch
            {
                // Fall back to text processing if image rendering fails
            }
        }

        // Standard text-only paragraph processing
        var paraScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Paragraph");
        var (pfg, pbg, pfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(paraScopes), theme);

        var paraBuilder = new StringBuilder();
        InlineProcessor.ExtractInlineText(paragraph.Inline, theme, paraBuilder);

        if (paraBuilder.Length == 0 || string.IsNullOrWhiteSpace(paraBuilder.ToString()))
            return Text.Empty;

        // Apply the theme colors/style to the paragraph
        if (pfg != -1 || pbg != -1 || pfs != TextMateSharp.Themes.FontStyle.NotSet)
        {
            var paraColor = pfg != -1 ? StyleHelper.GetColor(pfg, theme) : Color.Default;
            var paraBgColor = pbg != -1 ? StyleHelper.GetColor(pbg, theme) : Color.Default;
            var paraDecoration = StyleHelper.GetDecoration(pfs);

            var paraStyle = new Style(paraColor, paraBgColor, paraDecoration);
            var styledBuilder = new StringBuilder();
            styledBuilder.AppendWithStyle(paraStyle, paraBuilder.ToString());
            return new Markup(styledBuilder.ToString());
        }
        else
        {
            return new Markup(paraBuilder.ToString());
        }
    }

    /// <summary>
    /// Checks if a paragraph contains any image links.
    /// </summary>
    /// <param name="paragraph">The paragraph to check</param>
    /// <param name="imageLinks">The found image links</param>
    /// <returns>True if the paragraph contains any images</returns>
    private static bool ContainsImages(ParagraphBlock paragraph, out List<LinkInline> imageLinks)
    {
        imageLinks = new List<LinkInline>();

        if (paragraph.Inline is null)
            return false;

        // Find all image links in the paragraph
        foreach (var inline in paragraph.Inline)
        {
            if (inline is LinkInline link && link.IsImage)
            {
                imageLinks.Add(link);
            }
        }

        return imageLinks.Count > 0;
    }    /// <summary>
    /// Renders a paragraph that contains images, handling both text and Sixel images.
    /// </summary>
    /// <param name="paragraph">The paragraph to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="imageLinks">The image links found in the paragraph</param>
    /// <returns>A renderable containing the mixed content</returns>
    private static IRenderable RenderParagraphWithImages(ParagraphBlock paragraph, Theme theme, List<LinkInline> imageLinks)
    {
        // If the paragraph contains only a single image, render it as a standalone image
        if (IsImageOnlyParagraph(paragraph, imageLinks))
        {
            var singleImage = imageLinks[0];
            return ImageRenderer.RenderImage(
                singleImage.Title ?? singleImage.Label ?? "Image",
                singleImage.Url ?? string.Empty);
        }

        // For mixed content, we need to create a composite layout
        var renderables = new List<IRenderable>();
        var currentTextBuilder = new StringBuilder();

        foreach (var inline in paragraph.Inline!)
        {
            if (inline is LinkInline link && link.IsImage)
            {
                // If we have accumulated text, add it first
                if (currentTextBuilder.Length > 0)
                {
                    var textContent = currentTextBuilder.ToString().Trim();
                    if (!string.IsNullOrEmpty(textContent))
                    {
                        // Apply paragraph styling to the text
                        var textMarkup = ApplyParagraphStyling(textContent, theme);
                        renderables.Add(textMarkup);
                    }
                    currentTextBuilder.Clear();
                }

                // Add the image as an inline element
                var imageRenderable = ImageRenderer.RenderImageInline(
                    link.Title ?? link.Label ?? "Image",
                    link.Url ?? string.Empty,
                    maxWidth: 60,  // Smaller for inline images
                    maxHeight: 20);
                renderables.Add(imageRenderable);
            }
            else
            {
                // Process non-image inline elements
                ProcessNonImageInline(inline, theme, currentTextBuilder);
            }
        }

        // Add any remaining text
        if (currentTextBuilder.Length > 0)
        {
            var textContent = currentTextBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(textContent))
            {
                var textMarkup = ApplyParagraphStyling(textContent, theme);
                renderables.Add(textMarkup);
            }
        }

        // If we only have one renderable, return it directly
        if (renderables.Count == 1)
        {
            return renderables[0];
        }

        // For multiple renderables, combine them in a layout that flows better
        // Use a Columns layout for better inline flow when possible
        if (renderables.Count <= 3 && renderables.All(r => r is Markup || IsCompactImage(r)))
        {
            try
            {
                return new Columns(renderables.ToArray());
            }
            catch
            {
                // Fall back to vertical layout if columns fail
            }
        }

        // Default to vertical layout for complex mixed content
        return new Rows(renderables);
    }

    /// <summary>
    /// Applies paragraph styling to text content.
    /// </summary>
    /// <param name="text">The text to style</param>
    /// <param name="theme">Theme for styling</param>
    /// <returns>Styled markup</returns>
    private static Markup ApplyParagraphStyling(string text, Theme theme)
    {
        var paraScopes = MarkdigTextMateScopeMapper.GetBlockScopes("Paragraph");
        var (pfg, pbg, pfs) = TokenProcessor.ExtractThemeProperties(new MarkdownToken(paraScopes), theme);

        // Apply the theme colors/style to the paragraph
        if (pfg != -1 || pbg != -1 || pfs != TextMateSharp.Themes.FontStyle.NotSet)
        {
            var paraColor = pfg != -1 ? StyleHelper.GetColor(pfg, theme) : Color.Default;
            var paraBgColor = pbg != -1 ? StyleHelper.GetColor(pbg, theme) : Color.Default;
            var paraDecoration = StyleHelper.GetDecoration(pfs);

            var paraStyle = new Style(paraColor, paraBgColor, paraDecoration);
            var styledBuilder = new StringBuilder();
            styledBuilder.AppendWithStyle(paraStyle, text);
            return new Markup(styledBuilder.ToString());
        }
        else
        {
            return new Markup(text);
        }
    }

    /// <summary>
    /// Checks if a renderable is a compact image suitable for inline display.
    /// </summary>
    /// <param name="renderable">The renderable to check</param>
    /// <returns>True if it's a compact image</returns>
    private static bool IsCompactImage(IRenderable renderable)
    {
        // Simple heuristic: if it's a markup with an image emoji, treat it as compact
        return renderable is Markup markup && markup.ToString()?.Contains("🖼️") == true;
    }

    /// <summary>
    /// Checks if a paragraph contains only a single image (and possibly whitespace).
    /// </summary>
    /// <param name="paragraph">The paragraph to check</param>
    /// <param name="imageLinks">The image links found in the paragraph</param>
    /// <returns>True if the paragraph contains only a single image</returns>
    private static bool IsImageOnlyParagraph(ParagraphBlock paragraph, List<LinkInline> imageLinks)
    {
        if (imageLinks.Count != 1)
            return false;

        if (paragraph.Inline is null)
            return false;

        // Get all non-whitespace inlines
        var nonWhitespaceInlines = paragraph.Inline
            .Where(inline => !(inline is LiteralInline literal && string.IsNullOrWhiteSpace(literal.Content.ToString())))
            .ToList();

        // Should have exactly one non-whitespace inline, which should be our image
        return nonWhitespaceInlines.Count == 1 && nonWhitespaceInlines[0] == imageLinks[0];
    }

    /// <summary>
    /// Processes a non-image inline element and adds it to the text builder.
    /// </summary>
    /// <param name="inline">The inline element to process</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="builder">StringBuilder to append to</param>
    private static void ProcessNonImageInline(Markdig.Syntax.Inlines.Inline inline, Theme theme, StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case LinkInline link when !link.IsImage:
                // Process regular links
                var linkBuilder = new StringBuilder();
                InlineProcessor.ExtractInlineText(link, theme, linkBuilder);
                builder.Append(linkBuilder);
                break;

            case EmphasisInline emph:
                var emphBuilder = new StringBuilder();
                InlineProcessor.ExtractInlineText(emph, theme, emphBuilder);
                builder.Append(emphBuilder);
                break;

            case CodeInline code:
                // Handle code inline directly since ExtractInlineText doesn't support CodeInline
                builder.Append('`');
                builder.Append(code.Content);
                builder.Append('`');
                break;

            case LineBreakInline:
                builder.Append('\n');
                break;

            default:
                if (inline is ContainerInline childContainer)
                {
                    var childBuilder = new StringBuilder();
                    InlineProcessor.ExtractInlineText(childContainer, theme, childBuilder);
                    builder.Append(childBuilder);
                }
                break;
        }
    }
}
