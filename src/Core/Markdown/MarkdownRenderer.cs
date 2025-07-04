using Markdig;
using Spectre.Console;
using Spectre.Console.Rendering;
using PwshSpectreConsole.TextMate.Core.Markdown.Renderers;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown;

/// <summary>
/// Main markdown renderer using Markdig for parsing and Spectre.Console for rendering.
/// Renders GitHub-style markdown as closely as possible in the console.
/// </summary>
internal static class MarkdownRenderer
{
    /// <summary>
    /// Renders markdown content using Markdig and Spectre.Console.
    /// </summary>
    /// <param name="markdown">Markdown text (can be multi-line)</param>
    /// <param name="theme">Theme object for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rows object for Spectre.Console rendering</returns>
    public static Rows Render(string markdown, Theme theme, ThemeName themeName)
    {
        var pipeline = CreateMarkdownPipeline();
        var document = Markdig.Markdown.Parse(markdown, pipeline);

        var rows = new List<IRenderable>();
        bool lastWasContent = false;

        for (int i = 0; i < document.Count; i++)
        {
            var block = document[i];
            var renderable = BlockRenderer.RenderBlock(block, theme, themeName);

            if (renderable is not null)
            {
                // Add spacing before certain block types or when there was previous content
                bool needsSpacing = ShouldAddSpacing(block, lastWasContent);

                if (needsSpacing && rows.Count > 0)
                {
                    rows.Add(Text.Empty);
                }

                rows.Add(renderable);
                lastWasContent = true;
            }
            else
            {
                lastWasContent = false;
            }
        }

        return new Rows([.. rows]);
    }

    /// <summary>
    /// Creates the Markdig pipeline with all necessary extensions enabled.
    /// </summary>
    /// <returns>Configured MarkdownPipeline</returns>
    private static MarkdownPipeline CreateMarkdownPipeline()
    {
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
    /// Determines if spacing should be added before a block element.
    /// </summary>
    /// <param name="block">The current block being rendered</param>
    /// <param name="lastWasContent">Whether the previous element was content</param>
    /// <returns>True if spacing should be added</returns>
    private static bool ShouldAddSpacing(Markdig.Syntax.Block block, bool lastWasContent)
    {
        return lastWasContent ||
               block is Markdig.Syntax.HeadingBlock ||
               block is Markdig.Syntax.FencedCodeBlock ||
               block is Markdig.Extensions.Tables.Table ||
               block is Markdig.Syntax.QuoteBlock;
    }
}
