using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Optimized block renderer that uses Spectre.Console object building instead of markup strings.
/// This eliminates VT escaping issues and improves performance by avoiding double-parsing.
/// </summary>
internal static class OptimizedBlockRenderer
{
    /// <summary>
    /// Routes block elements to their appropriate optimized renderers.
    /// All renderers build Spectre.Console objects directly instead of markup strings.
    /// </summary>
    /// <param name="block">The block element to render</param>
    /// <param name="theme">Theme for styling</param>
    /// <param name="themeName">Theme name for TextMateProcessor</param>
    /// <returns>Rendered block as a Spectre.Console object, or null if unsupported</returns>
    public static IRenderable? RenderBlock(Block block, Theme theme, ThemeName themeName)
    {
        return block switch
        {
            // Use optimized renderers that build Spectre.Console objects directly
            HeadingBlock heading => OptimizedHeadingRenderer.Render(heading, theme),
            ParagraphBlock paragraph => OptimizedParagraphRenderer.Render(paragraph, theme),
            ListBlock list => OptimizedListRenderer.Render(list, theme),
            Table table => OptimizedTableRenderer.Render(table, theme),
            FencedCodeBlock fencedCode => OptimizedCodeBlockRenderer.RenderFencedCodeBlock(fencedCode, theme, themeName),
            CodeBlock indentedCode => OptimizedCodeBlockRenderer.RenderCodeBlock(indentedCode, theme),

            // Keep existing renderers for remaining complex blocks
            QuoteBlock quote => QuoteRenderer.Render(quote, theme),
            HtmlBlock html => HtmlBlockRenderer.Render(html, theme, themeName),
            ThematicBreakBlock => HorizontalRuleRenderer.Render(),

            // Unsupported block types
            _ => null
        };
    }
}
