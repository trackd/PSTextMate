using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Visitor-pattern renderer for Markdown heading blocks.
/// Renders headings with level-based styling using TextMate themes.
/// </summary>
internal class SpectreHeadingRenderer : SpectreMarkdownObjectRenderer<HeadingBlock> {
    private readonly Theme _theme;
    private readonly ThemeName _themeName;
    public SpectreHeadingRenderer(Theme theme, ThemeName themeName) {
        _theme = theme;
        _themeName = themeName;
    }

    protected override IRenderable Render(HeadingBlock heading) =>
        // Delegate to existing static implementation for now
        // This maintains compatibility while adopting visitor pattern
        HeadingRenderer.Render(heading, _theme);
}
