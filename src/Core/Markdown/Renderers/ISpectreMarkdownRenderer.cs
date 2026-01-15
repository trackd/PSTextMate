using Markdig.Syntax;
using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Base interface for Spectre.Console markdown object renderers.
/// Follows the visitor pattern for extensible rendering.
/// </summary>
public interface ISpectreMarkdownRenderer {
    /// <summary>
    /// Determines if this renderer handles the given object type.
    /// </summary>
    bool Accept(Type objectType);

    /// <summary>
    /// Renders a markdown object to a Spectre renderable.
    /// </summary>
    IRenderable Render(MarkdownObject obj);
}
