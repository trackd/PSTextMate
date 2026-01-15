using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown quote blocks.
/// </summary>
internal class SpectreQuoteRenderer : SpectreMarkdownObjectRenderer<QuoteBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;

    public SpectreQuoteRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(QuoteBlock quote) => QuoteRenderer.Render(quote, _theme);
}
