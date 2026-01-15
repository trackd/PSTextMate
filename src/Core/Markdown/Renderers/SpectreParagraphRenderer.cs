using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown paragraph blocks.
/// </summary>
internal class SpectreParagraphRenderer : SpectreMarkdownObjectRenderer<ParagraphBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;

    public SpectreParagraphRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(ParagraphBlock paragraph)
        => ParagraphRenderer.Render(paragraph, _theme);
}
