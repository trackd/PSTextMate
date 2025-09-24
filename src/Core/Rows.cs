using Spectre.Console.Rendering;

namespace PwshSpectreConsole.TextMate.Core;

/// <summary>
/// Container for rendered rows returned by renderers.
/// </summary>
public sealed record Rows(IRenderable[] Renderables)
{
    public static Rows Empty { get; } = new Rows(Array.Empty<IRenderable>());
}
