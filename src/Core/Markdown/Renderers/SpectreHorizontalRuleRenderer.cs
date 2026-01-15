using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Renders thematic break blocks (horizontal rules) using the visitor pattern.
/// </summary>
internal class SpectreHorizontalRuleRenderer : SpectreMarkdownObjectRenderer<ThematicBreakBlock> {
    public SpectreHorizontalRuleRenderer() {
    }

    protected override IRenderable Render(ThematicBreakBlock block)
        => HorizontalRuleRenderer.Render();
}
