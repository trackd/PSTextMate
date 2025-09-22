using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Block renderer that uses Spectre.Console object building instead of markup strings.
/// This eliminates VT escaping issues and improves performance by avoiding double-parsing.
/// </summary>
internal static class BlockRenderer
{
    /// <summary>
    /// Routes block elements to their appropriate renderers.
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
}
