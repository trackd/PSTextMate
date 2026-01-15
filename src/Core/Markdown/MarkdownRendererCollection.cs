using Markdig.Syntax;
using Spectre.Console.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

namespace PwshSpectreConsole.TextMate.Core.Markdown;

/// <summary>
/// Collection of markdown renderers implementing the visitor pattern.
/// Dispatches markdown objects to appropriate renderers based on type.
/// </summary>
internal class MarkdownRendererCollection {
    private readonly List<ISpectreMarkdownRenderer> _renderers = [];

    /// <summary>
    /// Initializes the renderer collection with all standard block renderers.
    /// </summary>
    public MarkdownRendererCollection(Theme theme, ThemeName themeName) {
        // Register standard block renderers
        _renderers.Add(new SpectreHeadingRenderer(theme, themeName));
        _renderers.Add(new SpectreParagraphRenderer(theme, themeName));
        _renderers.Add(new SpectreCodeBlockRenderer(theme, themeName));
        _renderers.Add(new SpectreQuoteRenderer(theme, themeName));
        _renderers.Add(new SpectreListRenderer(theme, themeName));
        _renderers.Add(new SpectreTableRenderer(theme, themeName));
        _renderers.Add(new SpectreHtmlBlockRenderer(theme, themeName));
        _renderers.Add(new SpectreHorizontalRuleRenderer());
    }

    /// <summary>
    /// Finds and uses the appropriate renderer for a markdown object.
    /// </summary>
    /// <returns>Rendered object, or null if no renderer found</returns>
    public IRenderable? Render(MarkdownObject obj) {
        if (obj is null) return null;

        // Find first renderer that accepts this object type
        ISpectreMarkdownRenderer? renderer = _renderers.FirstOrDefault(r => r.Accept(obj.GetType()));

        if (renderer is not null) return renderer.Render(obj);

        // Log unmapped type for debugging
        // System.Diagnostics.Debug.WriteLine(
        //     $"No renderer registered for type {obj.GetType().Name}");

        return null;
    }

    /// <summary>
    /// Adds a custom renderer to the collection.
    /// Allows third-party extensions to add support for new block types.
    /// </summary>
    public void Add(ISpectreMarkdownRenderer renderer) {
        if (renderer != null)
            _renderers.Add(renderer);
    }
}
