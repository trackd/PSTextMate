using Markdig.Extensions.Tables;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown table blocks.
/// </summary>
internal class SpectreTableRenderer : SpectreMarkdownObjectRenderer<Table> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;

    public SpectreTableRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(Table table) => TableRenderer.Render(table, _theme)!;
}
