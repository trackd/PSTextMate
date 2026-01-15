using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown list blocks (ordered and unordered).
/// </summary>
internal class SpectreListRenderer : SpectreMarkdownObjectRenderer<ListBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;
    public SpectreListRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(ListBlock list)
        => ListRenderer.Render(list, _theme);
}
