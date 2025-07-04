using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Main block renderer that dispatches to specific block type renderers.
/// </summary>
internal static class BlockRenderer
{
    /// <summary>
    /// Renders a Markdig block element to a Spectre.Console renderable.
    /// </summary>
    /// <param name="block">The block element to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rendered element or null if not supported</returns>
    public static IRenderable? RenderBlock(Block block, Theme theme, ThemeName themeName)
    {
        return block switch
        {
            HeadingBlock heading => HeadingRenderer.Render(heading, theme),
            ParagraphBlock paragraph => ParagraphRenderer.Render(paragraph, theme),
            ListBlock list => ListRenderer.Render(list, theme),
            FencedCodeBlock fencedCode => CodeBlockRenderer.RenderFencedCodeBlock(fencedCode, theme, themeName),
            CodeBlock code => CodeBlockRenderer.RenderCodeBlock(code, theme),
            Markdig.Extensions.Tables.Table table => TableRenderer.Render(table, theme),
            QuoteBlock quote => QuoteRenderer.Render(quote, theme),
            HtmlBlock htmlBlock => HtmlBlockRenderer.Render(htmlBlock, theme, themeName),
            ThematicBreakBlock => HorizontalRuleRenderer.Render(),
            _ => null
        };
    }
}
