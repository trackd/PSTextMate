using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PwshSpectreConsole.TextMate.Core.Markdown.Renderers;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown;

/// <summary>
/// Markdown renderer that builds Spectre.Console objects directly instead of markup strings.
/// This eliminates VT escaping issues and avoids double-parsing overhead for better performance.
/// </summary>
internal static class MarkdownRenderer {
    /// <summary>
    /// Renders markdown content using Spectre.Console object building.
    /// This approach eliminates VT escaping issues and improves performance.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Array of renderables for Spectre.Console rendering</returns>
    public static IRenderable[] Render(string markdown, Theme theme, ThemeName themeName) {
        MarkdownPipeline? pipeline = CreateMarkdownPipeline();
        MarkdownDocument? document = Markdig.Markdown.Parse(markdown, pipeline);

        var rows = new List<IRenderable>();
        Block? lastBlock = null;

        for (int i = 0; i < document.Count; i++) {
            Block? block = document[i];

            // Use block renderer that builds Spectre.Console objects directly
            IRenderable? renderable = BlockRenderer.RenderBlock(block, theme, themeName);

            if (renderable is not null) {
                // Determine if spacing is needed before current block
                // Add spacing when transitioning:
                // - FROM visual (tables, images, code) TO non-visual (text, headings, lists)
                // - FROM non-visual TO visual
                // But NOT between two visual blocks (they have their own styling)
                bool isCurrentVisual = HasVisualStyling(block);
                bool isLastVisual = lastBlock is not null && HasVisualStyling(lastBlock);

                bool needsSpacing = false;
                if (lastBlock is not null) {
                    // Visual to non-visual: add spacing after the visual element
                    if (isLastVisual && !isCurrentVisual) {
                        needsSpacing = true;
                    }
                    // Non-visual to visual: add spacing before the visual element
                    else if (!isLastVisual && isCurrentVisual) {
                        needsSpacing = true;
                    }
                    // Non-visual to non-visual: add spacing (paragraph to heading, etc)
                    else if (!isLastVisual && !isCurrentVisual) {
                        needsSpacing = true;
                    }
                    // Visual to visual: no spacing (they handle their own styling)
                }

                if (needsSpacing && rows.Count > 0) {
                    rows.Add(Text.Empty);
                }

                rows.Add(renderable);
                lastBlock = block;
            }
        }

        return [.. rows];
    }

    /// <summary>
    /// Creates the Markdig pipeline with all necessary extensions enabled.
    /// </summary>
    /// <returns>Configured MarkdownPipeline</returns>
    private static MarkdownPipeline CreateMarkdownPipeline() {
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .EnableTrackTrivia() // Enable HTML support
            .Build();
    }

    /// <summary>
    /// Determines if a block element has visual styling/borders that provide separation.
    /// These blocks don't need extra spacing as they're visually distinct.
    /// </summary>
    private static bool HasVisualStyling(Block? block) {
        return block is not null &&
            (block is Markdig.Extensions.Tables.Table ||
                block is FencedCodeBlock ||
                block is CodeBlock ||
                block is QuoteBlock ||
                block is HtmlBlock ||
                block is ThematicBreakBlock ||
                (block is ParagraphBlock para && IsStandaloneImage(para)));
    }

    /// <summary>
    /// Checks if a paragraph block contains only a single image (no other text).
    /// </summary>
    private static bool IsStandaloneImage(ParagraphBlock paragraph) {
        if (paragraph.Inline is null) {
            return false;
        }

        var inlines = paragraph.Inline.ToList();

        // Single image case
        if (inlines.Count == 1 && inlines[0] is LinkInline link && link.IsImage) {
            return true;
        }

        // Filter out empty/whitespace literals
        var nonWhitespace = inlines
            .Where(i => i is not LineBreakInline && !(i is LiteralInline lit && string.IsNullOrWhiteSpace(lit.Content.ToString())))
            .ToList();

        return nonWhitespace.Count == 1
            && nonWhitespace[0] is LinkInline imageLink
            && imageLink.IsImage;
    }
}
