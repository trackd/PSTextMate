using Markdig;
using System;
using System.IO;
using Markdig.Helpers;
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
/// Supports both traditional switch-based rendering and visitor pattern for extensibility.
/// </summary>
internal static class MarkdownRenderer {
    /// <summary>
    /// Renders markdown content using the visitor pattern with extensible renderer collection.
    /// Allows third-party extensions to add custom block renderers.
    /// Wraps all blocks in a Rows renderable to ensure proper spacing between them.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <param name="rendererCollection">Optional custom renderer collection (uses default if null)</param>
    /// <returns>Single Rows renderable containing all markdown blocks</returns>
    public static IRenderable RenderWithVisitorPattern(
        string markdown,
        Theme theme,
        ThemeName themeName,
        MarkdownRendererCollection? rendererCollection = null) {

        // Use cached pipeline for better performance
        MarkdownDocument? document = Markdig.Markdown.Parse(markdown, MarkdownPipelines.Standard);

        // Create renderer collection if not provided
        rendererCollection ??= new MarkdownRendererCollection(theme, themeName);

        var blocks = new List<IRenderable>();

        for (int i = 0; i < document.Count; i++) {
            Block? block = document[i];

            // Skip redundant paragraph that Markdig sometimes produces on the same line as a table
            if (block is ParagraphBlock && i + 1 < document.Count) {
                Block nextBlock = document[i + 1];
                if (nextBlock is Markdig.Extensions.Tables.Table table && block.Line == table.Line) {
                    continue;
                }
            }

            // Use visitor pattern to dispatch to appropriate renderer
            IRenderable? renderable = rendererCollection.Render(block);

            if (renderable is not null) {
                blocks.Add(renderable);
            }
        }

        // Wrap all blocks in Rows to ensure proper line breaks between them
        return new Rows([.. blocks]);
    }

    /// <summary>
    /// Renders markdown content using Spectre.Console object building.
    /// This approach eliminates VT escaping issues and improves performance.
    /// Uses traditional switch-based dispatch for compatibility.
    /// Wraps all blocks in a Rows renderable to ensure proper spacing between them.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Single Rows renderable containing all markdown blocks with proper spacing</returns>
    public static IRenderable Render(string markdown, Theme theme, ThemeName themeName) {
        // Use cached pipeline for better performance
        MarkdownDocument? document = Markdig.Markdown.Parse(markdown, MarkdownPipelines.Standard);

        var blocks = new List<IRenderable>();
        Block? previousBlock = null;

        for (int i = 0; i < document.Count; i++) {
            Block? block = document[i];

            // Skip redundant paragraph that Markdig sometimes produces on the same line as a table
            if (block is ParagraphBlock && i + 1 < document.Count) {
                Block nextBlock = document[i + 1];
                if (nextBlock is Markdig.Extensions.Tables.Table table && block.Line == table.Line) {
                    continue;
                }
            }

            // Use block renderer that builds Spectre.Console objects directly
            IRenderable? renderable = BlockRenderer.RenderBlock(block, theme, themeName);

            if (renderable is not null) {
                // Preserve source gaps: add a single empty row when there is at least one blank line between blocks
                if (previousBlock is not null) {
                    int gapFromTrivia = block.LinesBefore?.Count ?? 0;
                    int gapFromLines = block.Line - previousBlock.Line - 1;
                    int gap = Math.Max(gapFromTrivia, gapFromLines);

                    if (gap > 0) {
                        blocks.Add(Text.Empty);
                    }
                }

                blocks.Add(renderable);
                previousBlock = block;

                // Add extra spacing after standalone images (sixel images need breathing room)
                if (block is ParagraphBlock para && IsStandaloneImage(para)) {
                    blocks.Add(Text.Empty);
                }
            }
        }

        // Wrap all blocks in Rows to ensure proper line breaks between them
        return new Rows([.. blocks]);
    }

    /// <summary>
    /// Determines how many empty lines preceded this block in the source markdown.
    /// Uses Markdig's trivia tracking (LinesBefore) which is enabled in our pipeline.
    /// </summary>
    private static int GetEmptyLinesBefore(Block block) {
        // LinesBefore contains the empty lines that occurred before this block
        // This is only populated when EnableTrackTrivia() is used in the pipeline
        List<StringSlice>? linesBefore = block.LinesBefore;

        // Don't add spacing before the first block
        if (block.Line == 1) {
            return 0;
        }

        // If LinesBefore is populated, return the count (we'll add ONE Text.Empty per block)
        return linesBefore?.Count ?? 0;
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

        return nonWhitespace.Count == 1
                && nonWhitespace[0] is LinkInline imageLink
                && imageLink.IsImage;
    }
}
