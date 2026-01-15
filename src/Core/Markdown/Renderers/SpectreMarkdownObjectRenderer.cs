using Markdig.Syntax;
using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core.Markdown.Renderers;

/// <summary>
/// Generic base class for markdown object renderers.
/// Implements type checking and dispatch logic.
/// Derived classes need only implement the type-specific Render method.
/// </summary>
public abstract class SpectreMarkdownObjectRenderer<TObject>
    : ISpectreMarkdownRenderer
    where TObject : MarkdownObject {
    /// <summary>
    /// Checks if this renderer accepts the given object type.
    /// </summary>
    public virtual bool Accept(Type objectType)
        => typeof(TObject).IsAssignableFrom(objectType);

    /// <summary>
    /// Renders a markdown object (dispatches to typed method).
    /// </summary>
    public IRenderable Render(MarkdownObject obj)
        => Render((TObject)obj);

    /// <summary>
    /// Override this method to implement type-specific rendering.
    /// </summary>
    protected abstract IRenderable Render(TObject obj);
}
