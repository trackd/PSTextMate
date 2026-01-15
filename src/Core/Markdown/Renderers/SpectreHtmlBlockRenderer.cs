using Markdig.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for HTML blocks in Markdown.
/// </summary>
internal class SpectreHtmlBlockRenderer : SpectreMarkdownObjectRenderer<HtmlBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;

    public SpectreHtmlBlockRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(HtmlBlock htmlBlock)
        => HtmlBlockRenderer.Render(htmlBlock, _theme, _themeName);
}
