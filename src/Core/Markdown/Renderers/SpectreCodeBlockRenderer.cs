using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown code blocks (fenced and indented).
/// </summary>
internal class SpectreCodeBlockRenderer : SpectreMarkdownObjectRenderer<CodeBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;

    public SpectreCodeBlockRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(CodeBlock codeBlock) {
        // CodeBlockRenderer has separate methods for FencedCodeBlock and regular CodeBlock
        return codeBlock is FencedCodeBlock fenced
            ? CodeBlockRenderer.RenderFencedCodeBlock(fenced, _theme, _themeName)
            : CodeBlockRenderer.RenderCodeBlock(codeBlock, _theme);
    }
}
